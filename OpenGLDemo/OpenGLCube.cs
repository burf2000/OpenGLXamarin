using System;

using Xamarin.Forms;

namespace OpenGLDemo
{
	public class OpenGLCube : ContentPage
	{
		public OpenGLCube ()
		{
			Content = new StackLayout { 
				Children = {
					new Label { Text = "Hello ContentPage" }
				}
			};
		}
	}
}


