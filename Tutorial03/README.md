#Tutorial 03

##Goals
 - Get a better understanding of the shaders' tasks.
 - Understand why we need 4x4 matrices (and not 3x3) to perform transformations.
 - Grasp the indexing scheme in `Mesh` and how edges in geometry require that vertices
   are passed through the vertex shader more than once.
 
##Get that 3D Look
In the tutorials so far the generated output looked rather flat. Even the 3D-tetrahedron in [Tutorial 02] (../Tutorial02) really looked rather
like a flat triangle. This has two reasons:

 1. The individual pixel's colors are not calculated in a way that makes the objects appear 3D.
 2. So far we used a parallel projection instead of a perspective projection to display 3D coordinates on 
    a 2D display.

While we will address reason 1 in one of the upcoming tutorials (and NOT in this one), we want to take a closer look at coordinate transformations.

###Shaders revisited
But before that let's revisit what we already know about the rendering pipeline and shaders. As we already know, a vertex shader is called for each
vertex passed through the rendering pipeline. One of the main goals a vertex shader has to accomplish is to transform coordinates from the
coordinate system they are in when they enter the rendering pipeline to a coordinate system that resembles the display's (two) main axes. 

![Vertex Shader: Transform model into screen coords] (_images/VertexShader.png)

Without being too exact you could say: The vertex shader transforms each vertex from model coordinates to screen coordinates. It's not quite
exact because, als you already observed, the vertex shader's output coordinate system ranges from -1 to 1 in both screen dimensions, x and y. 
"Real" screen coordinates would be 2D-indices to individual pixels. So the coordinate system that the vertex shader needs to produce 
coordinate values for is a somewhat "normalized" screen coordinate system where -1 means left (in x direction) or top (in y direction) 
and +1 means right (for x) or bottom (for y). In addition, very often the vertex shader also performs other tasks, like setting up values
needed in the lighting calculation, (bone) animation, just to name a few.

As soon as these "screen" coordinates are known for all three vertices of a triangle, the render pipeline can figure out which pixels need to 
be filled with color. Then for each of these pixels the pixel shader (provided by you, the programmer) is called. This process is called rasterization.

![Pixel Shader: Calculate a color for each pixel] (_images/PixelShader.png)

Once the rendering pipeline knows which of the screen`s pixels are covered by geometry (the dark pixels on the left side of the image above),
it can call the pixel shader to do its task and fill any of the pixels covered by geometry with an individually calculated color.

###Transformations = Matrices
So what the vertex shader needs to do is: Perform transformations on incoming coordinates. Typically, the tranformation performed on each 
vertex is a composition of a long list of individual simple transformations. Consider a car racing game where for a single frame the model 
of a wheel of the car should be rendered. Each vertex of the wheel model is passed into the pipeline in  the wheel's model 
coordinate system. From there it should be transformed into the coordinate system of the car's body, so at a translation and 
some rotations must be performed. The whole car is placed somewhere on the game's "World", so another translation and some 
rotations must be applied. The whole scene is seen from some virtual camera which is positioned and oriented somewhere within in 
the world, so to yield screen coordinates the inverted camera's position and orienation must be applied and at the end the 
generated image should be perspectively projected, so a projection transformation needs to applied that minimizes distances 
between vertices far away and magnifies distances between near vertices.

Since geometry typically consists of a huge amount of vertices, it is desirable to cummulate a long list of transformations into
one single resulting transformation that can be applied in one single step to each vertex. This is where matrices come into play.
As we saw in [Tutorial 01] (../Tutorial01), mathematically we can describe a transformation such as a rotation in matrix form. 
Matrix calculus is assosiative, which means I can either take a vertex, multiply it to the first of a long list of matrices, take
the result and multiply it with the second matrix and so on OR I can first multiply all matrices in the order of application and
have one resulting matrix which I can then apply to all vertices sharing the same transformation (because they are part of the same
model).

In FUSEE we're using column-order notation of matrix calculus where a vector is multiplied to the right side of a matrix. Thus, 
a list of matrices applied to a vector would be written as
```C#
M3 * M2 * M1 * v
```
where v is a vector and M1 is the first transformation to be applied to v and M3 the last transformation. So according to the
associative law instead of calculating each Matrix one by one like this
```C#
M3 * (M2 * (M1 * v))
```
you can first multiply all transformation matrices into one single resulting transformation matrix and apply this to the vector
```C#
  (M3 * M2 * M1) * v
=      MRes      * v  
```
The advantage in the second line is: if you have not only one v but hundrets of thousands of vertices that need to be transformed in the same
way, you save a lot of calculations.

So let's say a big thank you to the inventors of matrix calculation. BUT - there's one tiny drawback: The building blocks we want to
use to build our composite resulting transformations are:
 - Translation (changing positions)
 - Rotations
 - Scale (make objects bigger or smaller)
 - Projection (here: perspective projection making far objects appear small)
 
Unfortunately only two of these transformation types, rotation and scale, can be expressed with 3x3 matrices. Help is on the way:
If you use 4x4 matrices and apply some mathematical tricks how to make 3D vectors fourdimensional before applying matrix
calculations and bring them back to three dimensions afterwards, you can indeed express translations and perspective projection and
still have that nice feature called associative law.

We don't need to go into maths much deeper now. But you should know now why we operate with 4x4 matrices
although all we want to do is transform some 3D vertices.

##Normals
Open `Tutorial03.sln` in Visual Studio and look into the file [Core/Tutorial.cs] (Core/Tutorial.cs). The mesh has become a lot more
complex now. We want to display a cube. With what we already know, we should think a cube is made out of eight vertices and each of the six 
faces made out of two triangles, so twelve triangles hooked on eight vertices. But now we want to display different faces with different 
colors. To do this (and also prepare for a more accurate color calculation) we specify the face normals with the vertices. This way
the vertex shader can access the orienation of the face at that special vertex. BUT: Since each vertex of the cube is shared by three
faces we need to specify three normals for each vertex. This is not possible, so all we can do is to duplicate each vertex position 
two times. This way we have three "physical" vertices at each of the eight different vertex positions - in sum 24 vertices. 
The following image shows the indices of the 24 vertices in the `_mesh` indicated by the respective normal. 

![A Cube: Each vertex exists three times with three different normals] (_images/VertsAndNormals.png)

Besides the changed ```_mesh``` geometry there are three more slight changes applied to the code compared to the [completed Tutorial02 source code] (../Tutorial02Completed/Core/Tutorial.cs):

 1. The mouse input is only applied to the rotation angle if the left mouse button is presssed. 
 
 2. Touch input is processed as an alternative to mouse input to allow interactions on android devices.
 
 3. The geometry is scaled by 0.5 in each dimension in the vertex shader. Since the cube's vertices are all positioned one unit away from 
    the coordinate system's origin, we need to downscale the cube to avoid clipping part of it if it's rotated.
	
Build and run Tutorial 03 on your favorite platform and look at the result.

![The tutorial 03 in it's original state] (_images/Cube01.png)

###Practice
 - Take a look at `_mesh.Vertices` and `_mesh.Normal` and understand that one index in `_mesh.Triangles` identifies a pair of one vertex 
   and one normal. 
 - Try to match the contents of `_mesh.Vertices` and `_mesh.Normal` with the image above: For a given index find  the vertex and and normal
   in the image above and check that the image is correct from the vertex' and the normal's coordinates.
  
##Normal Shading
Now we need to pass through the normal information provided with the vertices. If a mesh contains normals, FUSEE already passes the 
normals on to the vertex shader through the reserved attribute ```fuNormal```. This is just the same way as a mesh's vertices are 
passed to the vertex shader through ```fuVertex```. Since this tutorial is not yet about lighting calculation, we just want the normal
to be present in the pixel shader. So we add a a variable ```varying normal``` to both, the vertex and the pixel shader and assignt to 
it the incoming normal from ```fuNormal``` in the vertex shader. In the pixel shader, we then can 'abuse' the information contained in 
```normal``` instead of the contents of ```modelpos``` as a pixel color. The following code snippet contains the overall changes applied 
to both, vertex and pixel shader.

```C#
	private const string _vertexShader = @"
		attribute vec3 fuVertex;
		attribute vec3 fuNormal;
		uniform float alpha;
		varying vec3 modelpos;
		varying vec3 normal;
		void main()
		{
			modelpos = fuVertex;
			normal = fuNormal;
			float s = sin(alpha);
			float c = cos(alpha);
			gl_Position = vec4(0.5 * (fuVertex.x * c - fuVertex.z * s), 
							   0.5 *  fuVertex.y, 
							   0.5 * (fuVertex.x * s + fuVertex.z * c),
							   1.0);
		}";

	private const string _pixelShader = @"
		#ifdef GL_ES
			precision highp float;
		#endif
		varying vec3 modelpos;
		varying vec3 normal;

		void main()
		{
			gl_FragColor = vec4(normal*0.5 + 0.5, 1);
		}";
```

Note that we did not dispose of ```modelpos``` at this point, although we don`t need the variable right now. You may delete it in both
shaders, if you wish. 

Build and run the changes to see how our cube geometry now has a unique single color applied to every face now.

![The shaders now display the normal information] (_images/Cube02.png)

##Matrix from outside
As we know now, we can compose complex transformations from simple "atomic" tranformations like translation, rotation and scale. Instead
of doing matrix calculations in the vertex shader (where we need the transformation), we compose one single matrix outside the vertex
shader and then apply that matrix to all incoming vertices belonging to one mesh.

So we rewrite our vertex shader to accept a uniform matrix variable that it will apply to the vertex it is called for. The datatype for
4x4 matrices in GLSL, our shader language is ```mat4```. The datatype defined by FUSEE for 4x4 matrices is ```float4x4```.

In the vertex shader, remove the ```uniform``` variable ```alpha``` which we used to hold the current rotation angle and replace it by a ```uniform``` 
variable called ```xform```:

```C#
		private const string _vertexShader = @"
			attribute vec3 fuVertex;
			attribute vec3 fuNormal;
			uniform mat4 xform;
			varying vec3 modelpos;
			varying vec3 normal;
			void main()
			{
				modelpos = fuVertex;
				normal = fuNormal;
				gl_Position = xform * vec4(fuVertex, 1.0);
			}";
```

Instead of handling ```alpha``` as a shader variable from the 'outside' C# code, we now need to handle ```xform```. Note that we do not completely delete ```_alpha```. Apply the following changes to the C# code:

 - Declaration:
   ```C#
		private IShaderParam _xformParam;
		private float4x4 _xform;
		private float _alpha;
   ```

 - Inside ```Init()```:
   ```C#
		if (Mouse.LeftButton || Touch.GetTouchActive(TouchPoints.Touchpoint_0))
			_alpha -= speed.x * 0.0001f;

		_xformParam = RC.GetShaderParam(shader, "xform");
		_xform = float4x4.Identity;
   ```
   Note that we changed the operation on alpha from ```+=``` to ```-=```! This is due to the fact that 
   from now on we will be using FUSEE`s matrix calculation methods which operate on a left-handed coordinate
   system instead of a right-handed coordinate system implicitely assumed by OpenGL.

 - Inside ```RenderAFrame()```:
   ```C#
		_xform = float4x4.CreateRotationY(_alpha) * float4x4.CreateScale(0.5f);
		RC.SetShaderParam(_xformParam, _xform);
   ```

###Practice
 - Understand the changes applied above










  
   
##Exercise
 - xxx
