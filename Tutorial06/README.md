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
   components present in the WuggyLand.fus file. This happens in the now [extended
   `OnMaterial()` method found in Tutorial.cs](Core/Tutorial.cs#L78-L108).
   
###Practice
 - Take a look at the new Pixel Shader and try to figure out what the new
   `uniform` parameters do. Compare the changes to the [Pixel Shader from the 
   previous tutorial](../Tutorial05Completed/Core/Assets/PixelShader.frag). Temporarily
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
    to render, which pixel from `Leaves.jpg` it should take as the `albedo`.
    
###Textures are `uniform` Shader Parameters
Everything that controlled the process how a vertex shader proceesses coordinates or how
a pixel shader calculates the color for a given screen pixel was passed into the shader 
as a `uniform` parameter. We have seen single `float` values, `float3` values (used as
colors)  and `float4x4` matrix values. 
 
Since a texture is _quite_ something that influences the way an output color should be
calcualted, it is a `uniform` parameter as well. Because there is much more  data behind 
such a `uniform` parameter than in the cases before, there are some things that are different
compared to 'ordinary' `uniform` parameters:

 - We want to be able to read the contents of a texture image from file.
 - We want to be able to upload the texture contents to the GPU memory and
   'address' it somehow when needed rather than uploading all the pixels contained
   in a texture evrey frame.

FUSEE has some functionality we can use to do this. Perform the following steps:

 - First of all, add `Leaves.jpg` to the `Fusee.Tutorial06.Core` project's Asset folder
   and set its properties to `Content` and `Copy if newer`.
 - In the constructor of our `Renderer` class, get the asset's contents as an instance
   of the [`ImageData` structure](https://github.com/FUSEEProjectTeam/Fusee/blob/develop/src/Base/Common/ImageData.cs).
   and use the `RenderContext.CreateTexture()` method to upload the image to the GPU and get an 
   identifier for it 
   
   ```C#
   ImageData leaves = AssetStorage.Get<ImageData>("Leaves.jpg");
   _leavesTexture = RC.CreateTexture(leaves);
   TextureParam = RC.GetShaderParam(shader, "texture");
   TexMixParam = RC.GetShaderParam(shader, "texmix");
   ```
   don't forget to declare the three fields used above at the class level of the `Renderer` class:
   ```C#
   private IShaderParam TextureParam;
   private IShaderParam TexMixParam;
   private ITexture _leavesTexture;
   ```
 - To be able to access the texture in the pixel shader, add two `uniform` variables to `PixelShader.frag`:
   ```C#
   uniform sampler2D texture;
   uniform float texmix;
   ```
   Note the datatype `sampler2D` (with capital ***D***) in comparison to the datatypes we already used for `uniform` parameters!
   
 - Now we want to read a color value out of the `texture`. This can be done using the `texture2D()`
   function declared in GLSL. The first parameter of `texture2D()` is the texture to read from. The
   second parameter ist a 2D coordinate where both dimensions may contain values from 0 to 1. We will
   simply pass (0, 0) denoting the lower left pixel of the texture image. In addition, we will use the `texmix`
   variable as a means to mix the color value passed in `albedo` with the color read from the texture.
   All in all, the resulting pixel shader should look like this:
   ```C#
    #ifdef GL_ES
        precision highp float;
    #endif
    varying vec3 viewpos;
    varying vec3 normal;
    uniform vec3 albedo;
    uniform float shininess;
    uniform float specfactor;
    uniform vec3 speccolor;
    uniform vec3 ambientcolor;
    uniform sampler2D texture;
    uniform float texmix;

    void main()
    {
        vec3 nnormal = normalize(normal);
        
        // Diffuse
        vec3 lightdir = vec3(0, 0, -1);
        float intensityDiff = dot(nnormal, lightdir);
        vec3 resultingAlbedo = (1.0-texmix) * albedo + texmix * vec3(texture2D(texture, vec2(0, 0)));

        // Specular
        float intensitySpec = 0.0;
        if (intensityDiff > 0.0)
        {
            vec3 viewdir = -viewpos;
            vec3 h = normalize(viewdir+lightdir);
            intensitySpec = specfactor * pow(max(0.0, dot(h, nnormal)), shininess);
        }

        gl_FragColor = vec4(ambientcolor + intensityDiff * resultingAlbedo + intensitySpec * speccolor, 1);
    }
   ```
   Note how the `resultingAlbedo` is now calculated as a mixture between the original `albedo` and the color
   at (0, 0) in `texture`.

 - Finally, in `OnMaterial`, check if there is a texture given (assuming that it's "Leaves.jpg") and set
   our `uniform` parameter `texture` to be the leaves image, as well as `texmix` to 0 or 1, depending 
   on the presence of a texure in the material.
   ```C#
    if (material.Diffuse.Texture == "Leaves.jpg")
    {
        RC.SetShaderParamTexture(TextureParam, _leafTexture);
        RC.SetShaderParam(TexMixParam, 1.0f);
    }
    else
    {
        RC.SetShaderParam(TexMixParam, 0.0f);
    }
   ```


###Texture Coordinates

   
    
     