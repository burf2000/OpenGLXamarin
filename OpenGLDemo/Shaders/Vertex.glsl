uniform mat4 uMVPMatrix;
attribute vec4 vColor;
attribute vec4 vPosition; 
varying vec4 color;
void main()   
{              
    color = vColor;
    gl_Position = uMVPMatrix * vPosition;
}