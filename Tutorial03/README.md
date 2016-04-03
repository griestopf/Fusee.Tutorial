#Tutorial 03

##Goals Tut 03
 - Get a better understanding of the shaders' tasks.
 - Understand why we need 4x4 matrices (and not 3x3) to perform transformations.
 - 
 
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
"Real" screen coordinates would be individual pixels. So the coordinate system that the vertex shader needs to produce coordinate values for is
a somewhat "normalized" screen coordinate system where -1 means left (in x direction) or top (in y direction) and +1 means right (for x) or
bottom (for y). In addition, very often the vertex shader also performs other tasks, like setting up values needed in the lighting calculation,
(bone) animation, just to name a few.

Once these coordinates are known for all three vertices of a triangle, the render pipeline can figure out which pixels need to be filled with 
color. Then for each of these pixels the pixel shader (provided by you, the programmer) is called. This process is called rasterization.

![Pixel Shader: Calculate a color for each pixel] (_images/Pixelhader.png)

The 
Once the rendering pipeline knows which of the 
screen`s pixels are covered by geometry, it can call the pixel shader to do its task and fill any of the pixels covered by geometry with an 
individually calculated color.


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

