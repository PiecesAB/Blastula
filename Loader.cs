using Blastula.Graphics;
using Blastula.Input;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Threading.Tasks;

namespace Blastula
{
	/// <summary>
	/// Loads the rest of the game from the beginning.
	/// </summary>
	public partial class Loader : Node
	{
		[Export] public AnimationPlayer exitAnimator;
		[Export] public Control shaderLoader;
		[Export] public Shader[] shaderCompileList;
		[Export] AudioStreamPlayer fatalErrorSound;
		[Export] AudioStreamPlayer successSound;
		[Export] public Label errorText;
		[Export] public Label successText;
		[Export] public Label progressText;
		[Export] public TextureProgressBar progressBar;
		private float progressCurrentPiece = 1f;
		private float progressCurrentBase = 0f;
		private Error loadError = Error.Ok;
		private bool loadComplete = false;

		private void SetProgressBar(float currProgress)
		{
			progressBar.Value = progressCurrentBase + currProgress * progressCurrentPiece;
		}

		private async Task LoadScene(string path, string description, bool instantiate = true)
		{
			SceneTree st = GetTree();
			Window window = st.Root;
			Godot.Collections.Array a = new Godot.Collections.Array { 0 };
			if (!ResourceLoader.Exists(path))
			{
				loadError = Error.FileBadPath;
				progressText.Text = $"There is no file at the {description} path.";
				return;
			}
			Error sLoadError = ResourceLoader.LoadThreadedRequest(path, "PackedScene");
			if (sLoadError != Error.Ok)
			{
				loadError = sLoadError;
				progressText.Text = $"Couldn't load the {description} due to '{sLoadError}'.";
				return;
			}
			while (ResourceLoader.LoadThreadedGetStatus(path, a) == ResourceLoader.ThreadLoadStatus.InProgress)
			{
				progressText.Text = $"Loading {description}";
				SetProgressBar(a[0].AsSingle());
				await ToSignal(st, SceneTree.SignalName.ProcessFrame);
			}
			if (ResourceLoader.LoadThreadedGetStatus(path, a) == ResourceLoader.ThreadLoadStatus.InvalidResource)
			{
				loadError = Error.CantAcquireResource;
				progressText.Text = $"The {description} resource was invalid.";
				return;
			}
			else if (ResourceLoader.LoadThreadedGetStatus(path, a) == ResourceLoader.ThreadLoadStatus.Failed)
			{
				loadError = Error.Failed;
				progressText.Text = $"The {description} couldn't be loaded.";
				return;
			}
			if (instantiate)
			{
				Node newScene = ((PackedScene)ResourceLoader.LoadThreadedGet(path)).Instantiate();
				window.AddChild(newScene);
			}
		}

		/// <summary>
		/// Load a PackedScene from another script.
		/// </summary>
		public static void LoadExternal(Node dispatcher, string path)
		{
			PackedScene ps = (PackedScene)ResourceLoader.Load(path);
			Node n = ps.Instantiate();
			dispatcher.GetTree().Root.AddChild(n);
		}

		/// <summary>
		/// Load a PackedScene from another script.
		/// </summary>
		public static void LoadExternal(Node dispatcher, PackedScene directScene)
		{
			Node n = directScene.Instantiate();
			dispatcher.GetTree().Root.AddChild(n);
		}

		private async Task CompileShaders()
		{
			SceneTree st = GetTree();
			int runningCount = 0;
			foreach (Shader s in shaderCompileList)
			{
				progressText.Text = $"Pre-compiling shader {runningCount + 1}/{shaderCompileList.Length}";
				SetProgressBar(runningCount / (float)shaderCompileList.Length);
				((ShaderMaterial)shaderLoader.Material).Shader = s;
				await ToSignal(st, SceneTree.SignalName.ProcessFrame);
				++runningCount;
			}
		}

		public void ChangeToTitleScreen()
		{
			if (Engine.IsEditorHint()) { return; }
			Window window = GetTree().Root;
			string path = Persistent.TITLE_MENU_PATH;
			Node newScene = ((PackedScene)ResourceLoader.LoadThreadedGet(path)).Instantiate();
			window.AddChild(newScene);
			QueueFree();
		}

		public async Task LoadGame()
		{
			progressCurrentBase = 0f / 4f; progressCurrentPiece = 1f / 4f;
			await LoadScene(Persistent.KERNEL_PATH, "kernel");
			if (loadError != Error.Ok) { return; }
			progressCurrentBase = 1f / 4f; progressCurrentPiece = 1f / 4f;
			await LoadScene(Persistent.MAIN_SCENE_PATH, "main scene");
			if (loadError != Error.Ok) { return; }
			progressCurrentBase = 2f / 4f; progressCurrentPiece = 1f / 4f;
			await CompileShaders();
			if (loadError != Error.Ok) { return; }
			progressCurrentBase = 3f / 4f; progressCurrentPiece = 1f / 4f;
			await LoadScene(Persistent.TITLE_MENU_PATH, "title menu", false);
			if (loadError != Error.Ok) { return; }
			progressCurrentBase = 1f; progressCurrentPiece = 0f;
			SetProgressBar(1f);
			if (loadError == Error.Ok)
			{
				progressText.Text = $"Load complete";
				loadComplete = true;
				successSound.Play();
			}
		}

		public override void _Input(InputEvent input)
		{
			base._Input(input);
			if (input is InputEventMouse or InputEventMouseButton) return;
			if (input.IsPressed())
			{
				if (input is InputEventKey { Keycode: Key.Escape })
				{
					GD.Print("Setting input bindings to default");
					InputManager.main?.SwapControlToInputSet(InputManager.CurrentSet.Default);
					InputManager.main?.SaveFile();
				}

				if (loadError != Error.Ok)
				{
					GetTree().Quit(1);
				}
				else if (loadComplete)
				{
					exitAnimator.Active = true;
					exitAnimator.Play();
					exitAnimator.SpeedScale = 1;
				}
			}
		}


		private Task loadTask;
		public override void _Ready()
		{
			RenderingServer.GlobalShaderParameterAdd(
				BulletRendererManager.STAGE_TIME_NAME, 
				RenderingServer.GlobalShaderParameterType.Float, 
				0f
			);
			BulletRendererManager.stageTimeHasBeenAdded = true;
			errorText.Visible = false;
			successText.Visible = false;
			progressBar.Visible = true;
			loadTask = LoadGame();
		}

		public override void _Process(double delta)
		{
			base._Process(delta);
			if (loadError != Error.Ok)
			{
				if (loadTask.Exception != null)
				{
					loadError = Error.Failed;
					progressText.Text = loadTask.Exception.Message;
				}
				progressBar.Visible = false;
				errorText.Visible = true;
				if (fatalErrorSound != null && !fatalErrorSound.Playing)
				{
					fatalErrorSound.Play();
				}
			}
			else if (loadComplete)
			{
				progressBar.Visible = false;
				successText.Visible = true;
			}
		}
	}
}
