using Blastula.Input;
using Blastula.Sounds;
using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Blastula.Menus
{
	public partial class InputBindingMenu : BaseMenu
	{
		public enum Mode
		{
			Overview, BindingNow, Cancelled
		}

		public Mode mode = Mode.Overview;
		/// <summary>
		/// Root node of the title menu to delete when the game is ready to play.
		/// </summary>
		[Export] public Node root;
		[Export] public InputManager.CurrentSet highlightSet = InputManager.CurrentSet.A;
		[Export] public Control inputRowSample;
		[Export] public Control scrollerInside;
		[Export] public VScrollBar scrollBar;
		[Export] public AnimationPlayer headerAnim;
		[Export] public AnimationPlayer highlightAnim;
		[ExportGroup("Rebind")]
		[Export] public AnimationPlayer rebindAnimationPlayer;
		[Export] public Label rebindProgressCounter;
		[Export] public Control rebindTimeoutBar;
		private float rebindTimeoutStartWidth;
		[Export] public Label rebindNextInput;
		[Export] public Label rebindCancelLabel;
		[Export] public Godot.Collections.Dictionary<InputManager.RebindCancelReason, string> rebindCancelMessages = new();
		[Export] public Label rebindJustPressed;

		private List<Control> inputRows = new();
		private List<AnimationPlayer> inputRowAnims = new();
		private List<InputManager.InputInfo> loadedInputInfos = new();

		private float? cachedContainerHeight;
		private float GetContainerHeight()
		{
			if (cachedContainerHeight.HasValue) return cachedContainerHeight.Value;
			return (cachedContainerHeight = (inputRowSample.GetParent().GetParent() as Control).Size.Y).Value;
		}

		private float GetScrollHeight()
		{
			return Mathf.Max(0, inputRowSample.Size.Y * inputRows.Count - GetContainerHeight());
		}

		private void RecalculateInputRows()
		{
			for (int i = 0; i < loadedInputInfos.Count; ++i)
			{
				Control inputRow = inputRows[i];
				(inputRow.GetChild(1).GetChild(0) as Label).Text = loadedInputInfos[i].name;
				(inputRow.GetChild(2).GetChild(0) as Label).Text = loadedInputInfos[i].BindToDisplayString(0) ?? "";
				(inputRow.GetChild(3).GetChild(0) as Label).Text = loadedInputInfos[i].BindToDisplayString(1) ?? "";
				(inputRow.GetChild(4).GetChild(0) as Label).Text = loadedInputInfos[i].BindToDisplayString(2) ?? "";
				(inputRow.GetChild(5).GetChild(0) as Label).Text = loadedInputInfos[i].BindToDisplayString(3) ?? "";
			}
		}

		private void CreateInputRows()
		{
			loadedInputInfos = InputManager.GetLoadedInputInfos(false).Values.ToList();
			for (int i = 0; i < loadedInputInfos.Count; ++i)
			{
				Control newInputRow;
				if (i == 0) 
				{ 
					newInputRow = inputRowSample; 
				}
				else
				{
					newInputRow = inputRowSample.Duplicate(7) as Control;
					inputRowSample.GetParent().AddChild(newInputRow);
				}
				inputRows.Add(newInputRow);
				// more idiosyncratic structure.
				inputRowAnims.Add(newInputRow.GetChild(0) as AnimationPlayer);
				(newInputRow.GetChild(1).GetChild(0) as Label).Text = loadedInputInfos[i].name;
				(newInputRow.GetChild(2).GetChild(0) as Label).Text = loadedInputInfos[i].BindToDisplayString(0) ?? "";
				(newInputRow.GetChild(3).GetChild(0) as Label).Text = loadedInputInfos[i].BindToDisplayString(1) ?? "";
				(newInputRow.GetChild(4).GetChild(0) as Label).Text = loadedInputInfos[i].BindToDisplayString(2) ?? "";
				(newInputRow.GetChild(5).GetChild(0) as Label).Text = loadedInputInfos[i].BindToDisplayString(3) ?? "";
			}
			RecalculateInputRows();
		}

		private void ReplayOverviewAnims()
		{
			string currAnimString = InputManager.GetCurrentSet().ToString();
			headerAnim.Play(currAnimString);
			highlightAnim.Play(highlightSet.ToString());
			foreach (var inputRowAnim in inputRowAnims) inputRowAnim.Play(currAnimString);
		}

		public override void _Ready()
		{
			base._Ready();
			rebindTimeoutStartWidth = rebindTimeoutBar.Size.X;
			CreateInputRows();
			ReplayOverviewAnims();
			scrollBar.Page = GetContainerHeight() / (GetScrollHeight() + GetContainerHeight());
			Open();
		}

		public void PlayCommonSFX(string sfxName)
		{
			CommonSFXManager.PlayByName(sfxName);
		}

		public void StartRebind()
		{
			rebindAnimationPlayer.Play("Open");
			InputManager.main.StartRebinding(highlightSet, RebindingAfterInput, RebindingCancel, RebindingComplete);
			mode = Mode.BindingNow;
			PlayCommonSFX("Menu/Select");
		}

		public void RebindingAfterInput(InputEvent justBound, string nextName)
		{
			rebindJustPressed.Text = InputManager.InputInfo.BindToSerializedString(justBound) ?? "";
			rebindNextInput.Text = nextName ?? "";
			rebindAnimationPlayer.Stop();
			rebindAnimationPlayer.Play("Open");
			if (justBound != null) PlayCommonSFX("Menu/Type");
		}

		public void RebindingCancel(InputManager.RebindCancelReason reason)
		{
			mode = Mode.Cancelled;
			rebindCancelLabel.Text = rebindCancelMessages[reason];
			rebindAnimationPlayer.Play("Cancel");
			PlayCommonSFX("Menu/Back");
		}

		public void EndCancel()
		{
			mode = Mode.Overview;
			rebindAnimationPlayer.Play("Close");
		}

		public void RebindingComplete()
		{
			RecalculateInputRows();
			mode = Mode.Overview;
			rebindAnimationPlayer.Play("Close");
			ReplayOverviewAnims();
			PlayCommonSFX("Menu/Select");
		}

		public override void _Process(double delta)
		{
			base._Process(delta);

			if (mode == Mode.Overview)
			{
				if (InputManager.ButtonPressedThisFrame("Menu/Back"))
				{
					PlayCommonSFX("Menu/Back");
					Close();
					root.QueueFree();
					return;
				}

				bool changedHighlight = false;

				if (InputManager.ButtonPressedThisFrame("Menu/Left"))
				{
					highlightSet = (InputManager.CurrentSet)(((int)highlightSet + (int)InputManager.CurrentSet.Count - 1) % (int)InputManager.CurrentSet.Count);
					changedHighlight = true;
				}

				if (InputManager.ButtonPressedThisFrame("Menu/Right"))
				{
					highlightSet = (InputManager.CurrentSet)(((int)highlightSet + 1) % (int)InputManager.CurrentSet.Count);
					changedHighlight = true;
				}

				if (changedHighlight)
				{
					PlayCommonSFX("Menu/Switch");
					highlightAnim.Play(highlightSet.ToString());
				}

				float speed = 8f;
				bool scrolled = false;

				if (InputManager.ButtonIsDown("Menu/Down"))
				{
					speed *= Mathf.Min(2.5f, 1 + 0.025f * InputManager.GetButtonHeldFrames("Menu/Down"));
					scrollerInside.Position -= new Vector2(0, speed);
					scrolled = true;
				}

				if (InputManager.ButtonIsDown("Menu/Up"))
				{
					speed *= Mathf.Min(2.5f, 1 + 0.025f * InputManager.GetButtonHeldFrames("Menu/Up"));
					scrollerInside.Position += new Vector2(0, speed);
					scrolled = true;
				}

				if (scrolled)
				{
					scrollerInside.Position = new Vector2(scrollerInside.Position.X,
						Mathf.Clamp(
							scrollerInside.Position.Y,
							Mathf.Min(0f, -GetScrollHeight()),
							0f
						)
					);
					scrollBar.Value = (-scrollerInside.Position.Y / GetScrollHeight()) * (1f - scrollBar.Page);
				}

				if (InputManager.ButtonPressedThisFrame("Menu/Select"))
				{
					bool swapSucc = InputManager.main.SwapControlToInputSet(highlightSet);
					if (!swapSucc) StartRebind();
					else 
					{ 
						ReplayOverviewAnims();
						PlayCommonSFX("Menu/Select");
					}
				} 
				else if (InputManager.ButtonPressedThisFrame("Menu/Pause"))
				{
					if (highlightSet != InputManager.CurrentSet.Default) StartRebind();
				}
			}

			if (mode == Mode.BindingNow)
			{
				rebindProgressCounter.Text = InputManager.main.GetRebindProgressString();
				rebindTimeoutBar.Size = new Vector2(
					InputManager.main.GetProgressBeforeTimeout() * rebindTimeoutStartWidth,
					rebindTimeoutBar.Size.Y
				);
			}

			if (mode == Mode.Cancelled)
			{
				if (InputManager.ButtonPressedThisFrame("Menu/Select") || InputManager.ButtonPressedThisFrame("Menu/Back")
					|| rebindAnimationPlayer.CurrentAnimationLength - rebindAnimationPlayer.CurrentAnimationPosition < 0.01)
				{
					EndCancel();
				}
			}
		}
	}
}

