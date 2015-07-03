using System;
using OpenTK;
using Xamarin.Forms;
using OpenTK.Graphics.ES20;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Linq;
using OpenGLDemo;
using System.IO;
using System.Drawing;


#if __ANDROID__
	using Android.Util;
	using Android.App;
	using Android.Opengl;
	using Android.Graphics;
	using Android;
#elif __IOS__
	using UIKit;
	using Foundation;
	using CoreGraphics;
#endif

namespace OpenGLDemo
{
	public class myReferenceTime2
	{
		private myReferenceTime2()
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

	public class App2 : Xamarin.Forms.Application
	{

		public App2 ()
		{
			MainPage = new OpenGlTutoTextured { }; // your page here
		}
	}

	public class OpenGlTutoTextured : ContentPage
	{
		bool init_gl_done = false;

		private bool focus = false;
		private bool hidden_by_menu = false;

		int viewportWidth;
		int viewportHeight;

		int texture_handle = -1;
		int texture_sampler_handle = -1;

		// Vector4 to use quaternions
		Vector4 [] vertices;

		// Set color with red, green, blue and alpha (opacity) values
		Vector2 [] texture_coordinates;

		int mProgramHandle;
		int mTextureCoordinatesHandle;
		int mPositionHandle;
		int mMVPMatrixHandle;

		Matrix4 mProjectionMatrix;
		Matrix4 mViewMatrix;
		Matrix4 mModelViewProjectionMatrix;

		OpenGLView my3DView = null;

		public OpenGlTutoTextured()
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
//				while ((!focus) || (hidden_by_menu))
//				{
//					Thread.Sleep (500);
//				}

				if (!init_gl_done)
				{
					// get 3D view dimensions in pixels
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
				new Vector4(-0.5f,  0.5f, 0.0f, 1.0f),
				new Vector4( 0.5f,  0.5f, 0.0f, 1.0f),
				new Vector4(-0.5f, -0.5f, 0.0f, 1.0f),

				new Vector4( 0.5f,  0.5f, 0.0f, 1.0f),
				new Vector4( 0.5f, -0.5f, 0.0f, 1.0f),
				new Vector4(-0.5f, -0.5f, 0.0f, 1.0f)
			};

			// Set texture coordinates
			texture_coordinates = new Vector2 [] 
			{ 
				new Vector2(0.0f, 0.0f),
				new Vector2(1.0f, 0.0f),
				new Vector2(0.0f, 1.0f),

				new Vector2(1.0f, 0.0f),
				new Vector2(1.0f, 1.0f),
				new Vector2(0.0f, 1.0f)
			};

			// Vertex shader
			string vertexShaderSrc = @"
            uniform mat4 uMVPMatrix;
            attribute vec2 vTextureCoordinates;
            attribute vec4 vPosition; 
            varying vec2 varyingTextureCoordinates;
          
            void main()   
            {              
                varyingTextureCoordinates = vTextureCoordinates;
                gl_Position = uMVPMatrix * vPosition;
            }";


			// Fragment shader
			string fragmentShaderSrc = @"

            varying lowp vec2 varyingTextureCoordinates;
            uniform sampler2D texture_sampler;

            void main (void)
            {
                gl_FragColor = texture2D(texture_sampler, varyingTextureCoordinates);
            }
            ";

			int vertexShader = LoadShader (ShaderType.VertexShader, vertexShaderSrc );
			int fragmentShader = LoadShader (ShaderType.FragmentShader, fragmentShaderSrc );
			mProgramHandle = GL.CreateProgram();
			if (mProgramHandle == 0)
				throw new InvalidOperationException ("Unable to create program");

			GL.AttachShader (mProgramHandle, vertexShader);
			GL.AttachShader (mProgramHandle, fragmentShader);

			GL.BindAttribLocation (mProgramHandle, 0, "vPosition");
			GL.LinkProgram (mProgramHandle);

			GL.Viewport(0, 0, viewportWidth, viewportHeight);

			// Add program to OpenGL environment
			GL.UseProgram (mProgramHandle);

			texture_handle = LoadTexture("mario", 1, true, false);
			texture_sampler_handle = GL.GetUniformLocation(mProgramHandle, "texture_sampler");

			init_gl_done = true;
		}

		public static int LoadShader (ShaderType type, string shader_source)
		{
			int shader = GL.CreateShader (type); 
			if (shader == 0) 
			{
				throw new InvalidOperationException ("Unable to create shader"); 
			}

			int length = 0; 
			GL.ShaderSource (shader, 1, new string [] {shader_source}, (int[]) null); 
			GL.CompileShader (shader); 

			int compiled = 0; 
			GL.GetShader (shader, ShaderParameter.CompileStatus, out compiled); 
			if (compiled == 0) 
			{ 
				length = 0; 
				GL.GetShader (shader, ShaderParameter.InfoLogLength, out length); 
				if (length > 0) 
				{ 
					StringBuilder log = new StringBuilder(length); 
					GL.GetShaderInfoLog (shader, length, out length, log); 

					throw new InvalidOperationException ("GL2 : Couldn't compile shader: " + log.ToString ()); 
				} 

				GL.DeleteShader (shader); 
				throw new InvalidOperationException ("Unable to compile shader of type : " + type.ToString ()); 
			} 

			return shader; 
		}

		// see https://github.com/Clancey/Canvas/blob/master/Xamarin.Canvas.Android/Extension.cs
		// int id = GetId (typeof(Resource.Drawable), name);
		static int GetId (Type type, string propertyName) 
		{ 
			FieldInfo[] props = type.GetFields (); 
			FieldInfo prop = props.Select (p => p).Where (p => p.Name == propertyName).FirstOrDefault (); 
			if (prop != null)
			{
				return (int)prop.GetValue(type); 
			}
			return -1; 
		} 

		// see http://deathbyalgorithm.blogspot.fr/2013/05/opentk-textures.html
		public static int LoadTexture(string name, int quality, bool repeat, bool flip_y)
		{

			string prefix;

			#if __IOS__ 
			prefix = "OpenGLDemo.iOS.";
			#endif
			#if __ANDROID__
			prefix = "OpenGLDemo.Droid.";
			#endif

			var assembly = typeof(App2).GetTypeInfo ().Assembly;

//			foreach (var res in assembly.GetManifestResourceNames())
//				System.Diagnostics.Debug.WriteLine("found resource: " + res);

			Stream stream = assembly.GetManifestResourceStream (prefix + name + ".png");
			byte[] imageData;

			using (MemoryStream ms = new MemoryStream())
			{
				stream.CopyTo(ms);
				imageData = ms.ToArray();
			}

			#if __ANDROID__

				Bitmap b = BitmapFactory.DecodeByteArray (imageData, 0, imageData.Length);

			#elif __IOS__

				UIImage image = ImageFromByteArray (imageData);
				int width = (int)image.CGImage.Width;
				int height = (int)image.CGImage.Height;

				CGColorSpace colorSpace = CGColorSpace.CreateDeviceRGB ();
				byte[] imageData2 = new byte[height * width * 4];
				CGContext context = new CGBitmapContext (imageData2, width, height, 8, 4 * width, colorSpace,
					CGBitmapFlags.PremultipliedLast | CGBitmapFlags.ByteOrder32Big);

				colorSpace.Dispose ();
				context.ClearRect (new RectangleF (0, 0, width, height));
				context.DrawImage (new RectangleF (0, 0, width, height), image.CGImage);

			#endif

			int [] textures = new int[1];

			//Generate a new texture target in gl
			GL.GenTextures(1, textures);

			//Will bind the texture newly/empty created with GL.GenTexture
			//All gl texture methods targeting Texture2D will relate to this texture
			GL.BindTexture(TextureTarget.Texture2D, textures[0]);

			//The reason why your texture will show up glColor without setting these parameters is actually
			//TextureMinFilters fault as its default is NearestMipmapLinear but we have not established mipmapping
			//We are only using one texture at the moment since mipmapping is a collection of textures pre filtered
			//I'm assuming it stops after not having a collection to check.
			switch (quality)
			{
			case 1://High quality
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) All.Linear);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) All.Linear);
				break;
				//case 0:
			default://Low quality
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) All.Nearest);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) All.Nearest);
				break;
			}

			if (repeat)
			{
				//This will repeat the texture past its bounds set by TexImage2D
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) All.Repeat);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) All.Repeat);
			}
			else
			{
				//This will clamp the texture to the edge, so manipulation will result in skewing
				//It can also be useful for getting rid of repeating texture bits at the borders
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) All.ClampToEdge);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) All.ClampToEdge);
			}
				
			#if __ANDROID__
				GLUtils.TexImage2D ((int) All.Texture2D, 0, b, 0);
				b.Recycle();
			#elif __IOS__
				GL.TexImage2D (TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, imageData2);
			#endif

			GL.BindTexture(TextureTarget.Texture2D, 0);

			return textures[0];
		}
			
		// see http://www.opentk.com/node/2559
		public static void BindTexture(int textureId, TextureUnit textureUnit, int UniformId)
		{
			GL.ActiveTexture(textureUnit);
			GL.BindTexture(TextureTarget.Texture2D, textureId);
			GL.Uniform1(UniformId, textureUnit - TextureUnit.Texture0);
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

			BindTexture(texture_handle, TextureUnit.Texture0, texture_sampler_handle);

			mProjectionMatrix = Matrix4.CreateOrthographicOffCenter(-ratio, ratio, -1, 1, 0.1f, 10.0f);

			// Set the camera position (View matrix)
			mViewMatrix = Matrix4.LookAt(new Vector3(0, 0, 5), new Vector3(0, 0, 0), new Vector3(0, 1, 0));

			// Calculate the projection and view transformation
			mModelViewProjectionMatrix = Matrix4.Mult(mViewMatrix, mProjectionMatrix);

			Matrix4 mModel = Matrix4.CreateRotationY((float) (Math.PI * 2.0 * myReferenceTime2.GetTimeFromReferenceMs() / 5000.0));

			mModelViewProjectionMatrix = Matrix4.Mult(mModelViewProjectionMatrix, mModel);

			// get handle to vertex shader's vPosition member
			mPositionHandle = GL.GetAttribLocation(mProgramHandle, "vPosition");

			// get handle to vertex shader's vTextureCoordinates member
			mTextureCoordinatesHandle = GL.GetAttribLocation(mProgramHandle, "vTextureCoordinates");

			// Enable a handle to the triangle vertices
			GL.EnableVertexAttribArray (mPositionHandle);

			// Enable a handle to the triangle colors
			GL.EnableVertexAttribArray (mTextureCoordinatesHandle);

			unsafe 
			{
				fixed (Vector4* pvertices = vertices) 
				{
					// Prepare the triangle coordinate data
					GL.VertexAttribPointer (mPositionHandle, Vector4.SizeInBytes / 4, VertexAttribPointerType.Float, false, 0, new IntPtr (pvertices));
				} 

				fixed (Vector2* ptexturecoordinates = texture_coordinates) 
				{
					// Prepare the triangle color data
					GL.VertexAttribPointer (mTextureCoordinatesHandle, Vector2.SizeInBytes / 4, VertexAttribPointerType.Float, false, 0, new IntPtr (ptexturecoordinates));
				}
			}

			// get handle to shape's transformation matrix
			mMVPMatrixHandle = GL.GetUniformLocation(mProgramHandle, "uMVPMatrix");

			// Apply the projection and view transformation
			UniformMatrix4 (mMVPMatrixHandle, mModelViewProjectionMatrix);

			GL.DrawArrays (BeginMode.Triangles, 0, vertices.Length);

			GL.Finish ();

			// Disable vertex array
			GL.DisableVertexAttribArray(mPositionHandle);

			// Disable color array
			GL.DisableVertexAttribArray(mTextureCoordinatesHandle);
		}

		public void FocusChangeTo(bool new_focus)
		{
			if (new_focus != focus)
			{
				if (new_focus)
				{
					// do stuffs here
					focus = true;
				}
				else
				{
					focus = false;

					// do stuffs here
				}
			}
		}

		public void FocusHiddenByMenyTo(bool new_hidden_by_menu)
		{
			if (new_hidden_by_menu != hidden_by_menu)
			{
				if (new_hidden_by_menu)
				{
					// do stuffs here
					hidden_by_menu = true;
				}
				else
				{
					hidden_by_menu = false;
					// do stuffs here
				}
			}
		}

		#if __IOS__
		public static UIKit.UIImage ImageFromByteArray(byte[] data)
		{
		if (data == null) {
		return null;
		}

		UIKit.UIImage image;
		try {
		image = new UIKit.UIImage(Foundation.NSData.FromArray(data));
		} catch (Exception e) {
		Console.WriteLine ("Image load failed: " + e.Message);
		return null;
		}
		return image;
		}
		#endif
	}
}

