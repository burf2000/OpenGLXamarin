using System;
using OpenTK;
using Xamarin.Forms;
using OpenTK.Graphics.ES20;
using System.Text;
using System.Reflection;
using System.IO;

#if __ANDROID__
	using Android.Util;
	using Android.App;
#endif

// based on Xamarin triangle ES20 demo
namespace OpenGLDemo
{
	public class myReferenceTime
	{
		private myReferenceTime()
		{
		}

		static DateTime reference_time;
		static bool reference_time_set = false;

		public static double GetTimeFromReferenceMs()
		{
			if (!reference_time_set) 
			{
				reference_time = DateTime.Now;
				reference_time_set = true;
				return 0.0;
			}
			DateTime actual_time = DateTime.Now;
			TimeSpan ts = new TimeSpan(actual_time.Ticks - reference_time.Ticks);
			return ts.TotalMilliseconds;
		}

	}

	public class App : Xamarin.Forms.Application
	{

			public App ()
			{
				MainPage = new OpenGlTuto { }; // your page here
			}
	}

	public class OpenGlTuto : ContentPage
	{
		bool init_gl_done = false;

		int viewportWidth;
		int viewportHeight;

		// Vector4 to use quaternions
		Vector4 [] vertices;

		// Set color with red, green, blue and alpha (opacity) values
		Vector4 [] colors;

		uint mProgramHandle;
		int mColorHandle;
		int mPositionHandle;
		int mMVPMatrixHandle;

		Matrix4 mProjectionMatrix;
		Matrix4 mViewMatrix;
		Matrix4 mModelViewProjectionMatrix;

		OpenGLView my3DView = null;

		public OpenGlTuto()
		{
			Title = "Test";

			my3DView = new OpenGLView 
			{ 
				HeightRequest = 300,
				WidthRequest = 300,
				HasRenderLoop = true 
			};

			my3DView.OnDisplay = r => 
			{
				if (!init_gl_done)
				{
					#if __ANDROID__
						// get 3D view dimensions in pixels
						double width_in_pixels = TypedValue.ApplyDimension(ComplexUnitType.Dip, (float) my3DView.Width, Xamarin.Forms.Forms.Context.Resources.DisplayMetrics);
						double height_in_pixels = TypedValue.ApplyDimension(ComplexUnitType.Dip, (float) my3DView.Height, Xamarin.Forms.Forms.Context.Resources.DisplayMetrics);
					#elif __IOS__
						
						double width_in_pixels = 300;
						double height_in_pixels = 300;

					#endif
					InitGl((int) width_in_pixels, (int) height_in_pixels);
				}

				Render ();
			};

			var stack = new StackLayout 
			{ 
				Padding = new Xamarin.Forms.Size (20, 20),
				Children = {my3DView}
			};

			Content = stack;
		}

		void InitGl(int width, int height)
		{
			viewportHeight = width; 
			viewportWidth = height;

			// Set our triangle's vertices
			vertices = new Vector4 [] 
			{
				new Vector4(0.0f, 0.5f, 0.0f, 1.0f),
				new Vector4(0.5f, -0.5f, 0.0f, 1.0f),
				new Vector4(-0.5f, -0.5f, 0.0f, 1.0f)
			};

			// Set color with red, green, blue and alpha (opacity) values
			colors = new Vector4 [] 
			{ 
				new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
				new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
				new Vector4(0.0f, 0.0f, 1.0f, 1.0f)
			};

			uint vertexShader = CompileShader ("Vertex", ShaderType.VertexShader);
			uint fragmentShader = CompileShader ("Fragment", ShaderType.FragmentShader);

			mProgramHandle = (uint)GL.CreateProgram();
			if (mProgramHandle == 0)
				throw new InvalidOperationException ("Unable to create program");

			GL.AttachShader (mProgramHandle, vertexShader);
			GL.AttachShader (mProgramHandle, fragmentShader);

			GL.BindAttribLocation (mProgramHandle, 0, "vPosition");
			GL.LinkProgram (mProgramHandle);

			GL.Viewport(0, 0, viewportWidth, viewportHeight);

			// Add program to OpenGL environment
			GL.UseProgram (mProgramHandle);

			init_gl_done = true;
		}

		public static uint CompileShader(string shaderName, ShaderType shaderType){
			string prefix;

			#if __IOS__ 
				prefix = "OpenGLDemo.iOS.Shaders.";
			#endif
			#if __ANDROID__
				prefix = "OpenGLDemo.Droid.Shaders.";
			#endif

			var assembly = typeof(App).GetTypeInfo ().Assembly;

			foreach (var res in assembly.GetManifestResourceNames())
				System.Diagnostics.Debug.WriteLine("found resource: " + res);

			Stream stream = assembly.GetManifestResourceStream (prefix + shaderName + ".glsl");

			string shaderString;

			using (var reader = new StreamReader (stream)) {
				shaderString = reader.ReadToEnd ();
			}

			uint shaderHandle = (uint)GL.CreateShader (shaderType);
			GL.ShaderSource ((int)shaderHandle, shaderString);
			GL.CompileShader (shaderHandle);

			return shaderHandle;
		}

		public static void UniformMatrix4(int location, Matrix4 value)
		{
			GL.UniformMatrix4(location, 1, false, ref value.Row0.X);
		}

		void Render ()
		{
			GL.UseProgram (mProgramHandle);

			GL.ClearColor (0.7f, 0.7f, 0.7f, 1);
			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

			float aspectRatio = ((float) Width) / ((float) Height);

			float ratio = ((float) viewportWidth) / ((float) viewportHeight);

			mProjectionMatrix = Matrix4.CreateOrthographicOffCenter(-ratio, ratio, -1, 1, 0.1f, 10.0f);

			// Set the camera position (View matrix)
			mViewMatrix = Matrix4.LookAt(new Vector3(0, 0, 5), new Vector3(0, 0, 0), new Vector3(0, 1, 0));

			// Calculate the projection and view transformation
			mModelViewProjectionMatrix = Matrix4.Mult(mViewMatrix, mProjectionMatrix);

			Matrix4 mModel = Matrix4.CreateRotationY((float) (Math.PI * 2.0 * myReferenceTime.GetTimeFromReferenceMs() / 5000.0));

			mModelViewProjectionMatrix = Matrix4.Mult(mModelViewProjectionMatrix, mModel);

			// get handle to vertex shader's vPosition member
			mPositionHandle = GL.GetAttribLocation(mProgramHandle, "vPosition");

			// get handle to vertex shader's vColor member
			mColorHandle = GL.GetAttribLocation(mProgramHandle, "vColor");

			// Enable a handle to the triangle vertices
			GL.EnableVertexAttribArray (mPositionHandle);

			// Enable a handle to the triangle colors
			GL.EnableVertexAttribArray (mColorHandle);

			unsafe 
			{
				fixed (Vector4* pvertices = vertices) 
				{
					// Prepare the triangle coordinate data
					GL.VertexAttribPointer (mPositionHandle, Vector4.SizeInBytes / 4, VertexAttribPointerType.Float, false, 0, new IntPtr (pvertices));
				} 

				fixed (Vector4* pcolors = colors) 
				{
					// Prepare the triangle color data
					GL.VertexAttribPointer (mColorHandle, Vector4.SizeInBytes / 4, VertexAttribPointerType.Float, false, 0, new IntPtr (pcolors));
				}
			}

			// get handle to shape's transformation matrix
			mMVPMatrixHandle = GL.GetUniformLocation(mProgramHandle, "uMVPMatrix");

			// Apply the projection and view transformation
			UniformMatrix4 (mMVPMatrixHandle, mModelViewProjectionMatrix);

			GL.DrawArrays (BeginMode.Triangles, 0, 3);

			GL.Finish ();

			// Disable vertex array
			GL.DisableVertexAttribArray(mPositionHandle);

			// Disable color array
			GL.DisableVertexAttribArray (mColorHandle);
		}
	}

}

