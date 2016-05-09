#Tutorial 06

##Goals
 - See how textures can be made available in the pixel shader.
 - Understand texture coordinates as part of the geometry.
 - Understand the interplay between shaders, multiple render passes and render state. 
 
##Welcome to WuggyLand
Open the Tutorial 06 solution in Visual Studio and build and run it. A little interactive scene
has been created around the Wuggy rover. Using the `W`, `A`, `S` and `D` keys, 
you can now move the rover around the checkerboard-like ground plane.

![WuggyLand](_images/WuggyLand.png)

The entire scene is now contained in one single '.fus' file. All parts of 
the scene that need to be altered at run-time are identified by unique 
object names.
  
But let's look at the things that additionally changed under the hood.
 - The [Pixel Shader](Core/Assets/PixelShader.frag) now contains a more sophisticate
   specular color handling and an additional handling for setting an ambient color.  
 ```C# 
 uniform float specfactor;
 uniform vec3 speccolor;
 uniform vec3 ambientcolor;
 // ...
 intensitySpec = specfactor * pow(max(0.0, dot(h, nnormal)), shininess);
 gl_FragColor = vec4(ambientcolor + intensityDiff * albedo + intensitySpec * speccolor, 1);
 ```
 - These new parameters are set from respective entries in the various material
   components present in the WuggyLand.fus file. This happens in the now refurbished
   `OnMaterial()` method found in [Tutorial.cs](Core/Tutorial.cs#L78-L108).
   
###Practice
 - Take a look at the new Pixel Shader and try to figure out what the new
   `uniform` parameters do. Compare the changes to the [Pixel Shader we
   had so far] (../Tutorial05Completed/Core/Assets/PixelShader.frag). Temporarily
   comment out parts of the final color calculation to see their individual
   contribution.
 - Set a breakpoint within `OnMaterial()` and step through the various materials
   in `WuggyLand.fus`. Watch the `CurrentNode.Name` property to identify which material
   is used on which object and step into the various `if` and `else` clauses
   to see how different materials can be. 
 - Explain how the emissive component is used here. What would happen if our material
   handling / Pixel Shader ignored the emissive component?
 - Also watch the contents of the `material` component currently visited. What other 
   information is contained here which is currently NOT handled?
   
##Adding texture information
In `WuggyLand.fus`, the green parts of the tree models are already prepared
to show a very simple leaf-like structure by displaying an image spread 
over the rounded shapes. If you performed the last point of the Practice block 
above, you might have noticed that several material components contain a
non-null entry in the `Diffuse.Texture` property.

![DiffuseTexture](_images/TextureWatch.png)

This string property
contains a file name of an image: `Leaves.jpg`. You can take a look
at this image at its [location in the `Core/Assets` Subfolder](Core/Assets/Leaves.jpg)

![LeavesTexture](https://raw.githubusercontent.com/griestopf/Fusee.Tutorial/master/Tutorial06/Core/Assets/Leaves.jpg)

Now the material tells us to display this image on the green roundis treetop
models. To do this, we have to accomplish two things:

 1. Allow the PixelShader to access the pixels inside `Leaves.jpg`.
 2. Tell the PixelShader for each screen pixel (a.k.a "Fragment") it is about
    to render, which pixel from `Leaves.jpg` it should take as the `albedo`


    
     