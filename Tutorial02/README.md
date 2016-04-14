#Tutorial 02

##Goals
 - Understand how geometry is pumped through the rendering pipeline.
 - Understand `uniform`, `attribute` and `varying` shader variables.
 - Understand how data is passed from pixel to vertex shader.
 - Grasp basics of 3D transformations.

##Prerequisistes
 - Make sure you got [Tutorial01] (../Tutorial01) up and running.
 
##Passing more information through the pipeline
First, let's add some more triangles to the geometry. Add one more vertex and span four triangles with the four vertices to creatae a Tetrahedron ("a triangular pyramid"). Extend the Mesh instantiation in [Core/Tutorial.cs] (Core/Tutorial.cs) to the following (you may omit the comments):
```C#
	_mesh = new Mesh
	{
		Vertices = new[]
		{
			new float3(-0.8165f, -0.3333f, -0.4714f), // Vertex 0
			new float3(0.8165f, -0.3333f, -0.4714f),  // Vertex 1
			new float3(0, -0.3333f, 0.9428f),         // Vertex 2
			new float3(0, 1, 0),                      // Vertex 3
		},
		Triangles = new ushort[]
		{
			0, 2, 1,  // Triangle 0 "Bottom" facing towards negative y axis
			0, 1, 3,  // Triangle 1 "Back side" facing towards negative z axis
			1, 2, 3,  // Triangle 2 "Right side" facing towards positive x axis
			2, 0, 3,  // Triangle 3 "Left side" facing towrads negative x axis
		},
	};
```
Building and running the solution this way will still result in a single triangle being displayed. 
![Tetrahedron back side] (_images/TetrahedronBackSide.png)

This is Triangle 1, the "back side" of the tetrahedron. The other 
three triangles are obscured by the one we are seeing. Note that the visible triangle is now somewhat out of center towards the upper 
border of the window. This is because the vertex coordinates used above are chosen to make their common origin (0, 0, 0) to be the tetrahedron's center of gravity. 

##Rotating it
Now we would like to rotate the tetrahedron. There are two ways to accomplish this:
 1. We could change the Mesh's vertex coordinates every frame within `RenderAFrame`.
 2. We could transform the Mesh's vertex coordinates every frame from within the vertex shader.

The typical way to perform coordinate transformations is option 2, especially if we are performing linear transformations (such as a rotation in our case). 
Let's recall some maths to see how an arbitrary 2D vector (x, y) is rotated around an angle alpha to yield the new coordinates (x', y'):
```
x' = x * cos(alpha) + y * -sin(alpha)
y' = x * sin(alpha) + y *  cos(alpha)
```
From linear algebra you might remember that this can as well be written in matrix format. We will revisit matrices later on. Right now we want to apply the above 1:1 to 
our vertex shader:
```C#
	private const string _vertexShader = @"
		attribute vec3 fuVertex;

		void main()
		{
			float alpha = 3.1415 / 4; // (45 degrees)
			float s = sin(alpha);
			float c = cos(alpha);
			gl_Position = vec4( fuVertex.x * c - fuVertex.y * s,   // The transformed x coordinate
								fuVertex.x * s + fuVertex.y * c,   // The transformed y coordinate
								fuVertex.z,                        // z is unchanged
								1.0);
		}";

```
Building and running should result in the triangle rotated about 45 degrees in counterclockwise order.
![Tetrahedron 45 degrees] (_images/Tetrahedron45Deg.png)

###Practice
 - Try other amounts for alpha to see how the rotation behaves.
 - Go 3D! Rotate around the y-axis instead of the z-axis (that is, leave fuVertex.y unchanged and apply the sin/cos factors to x and z. This way you will 
   get to see the other sides of the tetrahedron. Unfortunately you cannot tell the border between the different sides because they are all white,
   so you'll always see a triangular silhouette.

   
## Animation
Notice that we now have a single parameter (`alpha`) controlling the transformation of all vertices within our mesh. This parameter is set to a constant value in
the vertex shader. If we could find a way to alter the value of `alpha` from one frame to the other, we could implement a rotation animation. Shader
languages allow to set individual values from the "outside world" through so called "uniform variables". Let's change `alpha` from a constant local variable
inside the vertex shader's `main` function to a more "global" uniform variable. Change the first lines of the vertex shader like this:

```C#
	private const string _vertexShader = @"
		attribute vec3 fuVertex;
		uniform float alpha;

		void main()
		{
			float s = sin(alpha);
	...
```
`alpha` now looks like a global variable (outside of `main`). In addition it is decorated with the `uniform` keyword, which marks it being a value 
that changes rather infrequently (the vertex shader will be called for a lot of vertices while `alpha`'s value doesn't change). This is in contrast
to the `fuVertex` variable on the line above which contains a different value (the vertex itself) for each time the vertex shader is called. Thus, 
this variable is marked being an `attribute` (and NOT a `uniform`)

Before we can change the value of `alpha` (which - as part of the vertex shader - lives on the GPU) from the application code (which runs on the CPU),
we need to have an identifier to access our variable. First we need to declare two fields within our `Tutorial` class:

```C#
	private IShaderParam _alphaParam;
	private float _alpha;
```

The filed `_alphaParam` will keep an identifier (a handle) to the shader variable `alpha` while the field `_alpha` will keep the actual value of 
the GPU-variable `alpha` in CPU-Land.

Inside the `Init` method, right after creating the shader from source code, we can initialize both fields:
```C#
	var shader = RC.CreateShader(_vertexShader, _pixelShader);
	RC.SetShader(shader);
	_alphaParam = RC.GetShaderParam(shader, "alpha");
	_alpha = 0;
```

Then, in the `RenderAFrame` method we can alter the contents of `_alpha` and then pass the new value up to the GPU's `alpha` variable:
```C#
	_alpha += 0.01f;
	RC.SetShaderParam(_alphaParam, _alpha);
```

This way, each frame the angle `alpha` will be incremented about 0.01 radians.

Building and running this will show a somewhat rotating triangle. If you rotate around the y-axis as proposed in the previus paragraph, you will 
rather recognize a triangle bouncing back and forth. Remember that you are really seeing the triangular silhouette of a rotating 
threedimensional tetrahedron. 

###Practice
 - Create a uniform variable in the pixel shader and do some color animation.

##Color
To get a more threedimensional graps of our geometry we want to add some more color to it. In later tutorials we will look at ways how to implement
lighting calculations producing more realism. For now, we just want our pixel shader to perform a simple calculation where the position is
interpreted as a color. To accomplish this, we need to pass the position information we receive in the vertex shader on to the pixel shader.

This is done through the `varying` variable `modelpos` which we need to declare in both shaders. Since our coorindates vary in the range from
-1 to 1 and we want to interprete the x, y and z coordinates as intensisites in the color channels r, g and be, we scale the values to the range
from 0 to 1 (this is done in the pixel shader `modelpos*0.5 + 0.5`).

```C#
	private const string _vertexShader = @"
		attribute vec3 fuVertex;
		uniform float alpha;
		varying vec3 modelpos;

		void main()
		{
			modelpos = fuVertex;
			float s = sin(alpha);
			float c = cos(alpha);
			gl_Position = vec4( fuVertex.x * c - fuVertex.z * s, 
								fuVertex.y, 
								fuVertex.x * s + fuVertex.z * c, 
								1.0);
		}";

	private const string _pixelShader = @"
		#ifdef GL_ES
			precision highp float;
		#endif
		varying vec3 modelpos;

		void main()
		{
			gl_FragColor = vec4(modelpos*0.5 + 0.5, 1);
		}";
```

Build and run these changes will show the tetrahedron's vertices each with a different color. But not only that: every place on the triangle has
a different color - the triangles' colors fade between the colors at their vertices. 

![Tetrahedron color] (_images/TetrahedronColor.png)

Why is that? - in our geometry we have defined only four vertices with four different positions. If we interpret positions as colors, shouldn't
we see only four different colors?

The answer is how the value we write into `modelpos` in the vertex shader arrives at the pixel shader when we read it back. Remember that 
the vertex shader is called for every vertex in our geometry. So it's called only four times. The pixel shader is called for every pixel that 
is covered by geometry. Depending on your screen resolution and the size of your window this might be several thousand to millon times. 

So there is no 1:1 relation of calls to the vertex shader and the pixel shader. Values that get passed from vertex to pixel shader are thus 
interpolated by the rendering pipeline. The interpolation for a `varying` value for an individual pixel when calling the pixel shader for that pixel
is based on the values that were set to that `varying` value at the three vertices with respect to the distance of the pixel in question to 
the vertices of the triangle it belongs to. So a pixel very close to a certain vertex gets a value very close to the value at that vertex.
A pixel very much in the middle of a triangle gets a value that is close to the mean of the value at the three triangle's vertices. 

In our case: Pixels close to the top of the pyramid are greener because the top vertex value (`float3(0, 1, 0)`) means "green" if interpreted
as r, g, b triple. Pixels farther from the top get less green, because they are influenced more and more by the other vertices.

The exact interpolation scheme applied here is based on so called "barycentric coordinates".

###Practice
 - Try more advanced mappings from x, y, z coordinates to r, g, b colors.
 - Understand that in the pixel shader `modelpos` now keeps the current model-coordinate of the pixel the shader is called for.
   Imagine ways how you could make use of this information.

##Interaction
Now that we know how to manipulate values in both, pixel and vertex shader, let's try to get interactive and see how we can read input values.
FUSEE allows easy akquisition of values from the standard input devices like mouse, keyboard and touch. The easiest way to access the 
`Fusee.Engine.Core.Input` class' static properties, `Mouse`, `Keyboard` and `Touch` is to add the following line to the top of the `Tutorial.cs` source code file:
```C#
    using static Fusee.Engine.Core.Input;
```

This way you can retrieve input from anywhere in your code (preferably from within `RenderAFrame`) by simply typing `Mouse.`, `Keyboard.` or `Touch.`
and see what IntelliSense/ReSharper offers you.

As an example, we would like to retrieve the current velocity of the mouse (the speed at which the user moves the mouse over our rendering window).
As the mouse position ist two-dimensional, we will retrieve the mouse speed as a two-dimensional vector with the vector's direction being the
direction the mouse is heading to and the speed in pixels/second indicated by the magnitude (the length). This value can be retieved in `RenderAFrame`
by
```C#
    float2 speed = Mouse.Velocity;
```
As an alternative (or additionally) you can retrieve the speed of the first touch point from a touch device using
```C#
    float2 speed = Touch.GetVelocity(TouchPoints.Touchpoint_0);
```
Instead of adding a constant value to `_alpha` we can now add an increment based on one of the speed axes. Let's take the x-axis:
```C#
    _alpha += speed.x * 0.0001f;
```
Note that we multiply `speed.x` with a very small factor. This is because angles are in radians, so one complete revolution is 
represented by a number slightly bigger than 6 (`2 * PI`), while the speed in pixels/second along the screen's x-axis can get rather 
big, especially with high resolution displays. 

Building and running this will give you interactive control over the rotation angle.

 - Visit the
   [result as web application] (https://cdn.rawgit.com/griestopf/Fusee.Tutorial/5658a54/Tutorial02Completed/out/Fusee.Tutorial.Web.html)
   (Ctrl-Click or Long-Press to open in new tab).
 
 - See [Tutorial.cs] (../Tutorial02Completed/Core/Tutorial.cs) in the [Tutorial02 Completed] (../Tutorial02Completed) folder for 
   the overall state so far.	


##Exercise
 - Create a more complex geometry (e.g. a little house)
 - Rotate around two axes and/or move along two axes controlled by input devices. You can use two-dimensional uniform variables. 
   The GLSL (shader language) data type for two-dimensional values is [vec2] (https://www.opengl.org/wiki/Data_Type_(GLSL))
 - Implement actions (like rotating) only working if the left mouse button is pressed (`Mouse.LeftButton`).
 - Try to combine the input from Mouse, Touch and Keyboard. To retrieve speed-like values from the keyboard, use the
   `LeftRightAxis` and `UpDownAxis`, or the `ADAxis` and `WSAxis` properties.
 - Extend your pixel shader to create a highlight on the geometry based on the mouse cursor position: The color should become lighter, the closer the 
   mouse cursor is to the pixel the pixel shader is called for. You will need to pass the mouse position (`uniform`ly) to the pixel shader and
   also need the (`varying`) pixel's position. To measure the distance between two 2D-Points (`vec2`) within a shader you can use the 
   [distance function] (https://www.opengl.org/sdk/docs/man/html/distance.xhtml).   

