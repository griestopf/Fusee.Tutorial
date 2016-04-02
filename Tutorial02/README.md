#Tutorial 02

##Goals
 - Understand how Geometry is pumped through the rendering pipeline.
 - Understand uniform shader variables.
 - Understand how data is passed from pixel to vertex shader.


 - Grasp basics of 3D transformations.
 - Understand why we need 4x4 matrices (and not 3x3) to perform transformations.

##Prerequisistes
 - Make sure you got [Tutorial01] (../Tutorial01) up and running.
 
##Passing more information through the pipeline
First, let's add some more triangles to the geometry. Add one more vertex and span four triangles with the four vertices to creatae a Tetrahedron ("a triangular pyramid").
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
Now we would like to rotate the tetrahedron. 

There are two ways to accomplish this:
 1 We could change the Mesh's vertex coordinates every frame within `RenderAFrame`.
 2 We could transform the Mesh's vertex coordinates every frame from within the vertex shader.
The typical way to perform coordinate transformations is option 2, especially if we are performing linear transformations (a rotation is a linear tranformation). 
Let's recall some maths to see how an arbitrary 2D vector (x, y) is rotated around an angle alpha:
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
 - Get 3D! Rotate around the y-axis instead of the z-axis (that is, leave fuVertex.y unchanged and apply the sin/cos factors to x and z. This way you will 
   get to see the other sides of the tetrahedron. Unfortunately you cannot tell the border between the different sides because they are all white,
   so you'll always see a triangular silhouette.

   
## Animation and interaction
Notice that we now have a single parameter (`alpha`) controlling the transformation of all vertices within our mesh.


##Practice
 - Create a more complex geometry (e.g. a little house)
 - Create 


