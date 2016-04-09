using Fusee.Base.Common;
using Fusee.Engine.Common;
using Fusee.Engine.Core;
using Fusee.Math.Core;
using static Fusee.Engine.Core.Input;

namespace Fusee.Tutorial.Core
{

    [FuseeApplication(Name = "Tutorial Example", Description = "The official FUSEE Tutorial.")]
    public class Tutorial : RenderCanvas
    {
        private Mesh _mesh;
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


        private IShaderParam _xformParam;
        private float4x4 _xform;
        private float _alpha;
        private float _beta;

        private float _yawCube1;
        private float _pitchCube1;
        private float _yawCube2;
        private float _pitchCube2;

        // Init is called on startup. 
        public override void Init()
        {
            _mesh = new Mesh
            {
                Vertices = new[]
                {
                    // left, down, front vertex
                    new float3(-1, -1, -1), // 0  - belongs to left
                    new float3(-1, -1, -1), // 1  - belongs to down
                    new float3(-1, -1, -1), // 2  - belongs to front

                    // left, down, back vertex
                    new float3(-1, -1,  1),  // 3  - belongs to left
                    new float3(-1, -1,  1),  // 4  - belongs to down
                    new float3(-1, -1,  1),  // 5  - belongs to back

                    // left, up, front vertex
                    new float3(-1,  1, -1),  // 6  - belongs to left
                    new float3(-1,  1, -1),  // 7  - belongs to up
                    new float3(-1,  1, -1),  // 8  - belongs to front

                    // left, up, back vertex
                    new float3(-1,  1,  1),  // 9  - belongs to left
                    new float3(-1,  1,  1),  // 10 - belongs to up
                    new float3(-1,  1,  1),  // 11 - belongs to back

                    // right, down, front vertex
                    new float3( 1, -1, -1), // 12 - belongs to right
                    new float3( 1, -1, -1), // 13 - belongs to down
                    new float3( 1, -1, -1), // 14 - belongs to front

                    // right, down, back vertex
                    new float3( 1, -1,  1),  // 15 - belongs to right
                    new float3( 1, -1,  1),  // 16 - belongs to down
                    new float3( 1, -1,  1),  // 17 - belongs to back

                    // right, up, front vertex
                    new float3( 1,  1, -1),  // 18 - belongs to right
                    new float3( 1,  1, -1),  // 19 - belongs to up
                    new float3( 1,  1, -1),  // 20 - belongs to front

                    // right, up, back vertex
                    new float3( 1,  1,  1),  // 21 - belongs to right
                    new float3( 1,  1,  1),  // 22 - belongs to up
                    new float3( 1,  1,  1),  // 23 - belongs to back

                },
                Normals = new[]
                {
                    // left, down, front vertex
                    new float3(-1,  0,  0), // 0  - belongs to left
                    new float3( 0, -1,  0), // 1  - belongs to down
                    new float3( 0,  0, -1), // 2  - belongs to front

                    // left, down, back vertex
                    new float3(-1,  0,  0),  // 3  - belongs to left
                    new float3( 0, -1,  0),  // 4  - belongs to down
                    new float3( 0,  0,  1),  // 5  - belongs to back

                    // left, up, front vertex
                    new float3(-1,  0,  0),  // 6  - belongs to left
                    new float3( 0,  1,  0),  // 7  - belongs to up
                    new float3( 0,  0, -1),  // 8  - belongs to front

                    // left, up, back vertex
                    new float3(-1,  0,  0),  // 9  - belongs to left
                    new float3( 0,  1,  0),  // 10 - belongs to up
                    new float3( 0,  0,  1),  // 11 - belongs to back

                    // right, down, front vertex
                    new float3( 1,  0,  0), // 12 - belongs to right
                    new float3( 0, -1,  0), // 13 - belongs to down
                    new float3( 0,  0, -1), // 14 - belongs to front

                    // right, down, back vertex
                    new float3( 1,  0,  0),  // 15 - belongs to right
                    new float3( 0, -1,  0),  // 16 - belongs to down
                    new float3( 0,  0,  1),  // 17 - belongs to back

                    // right, up, front vertex
                    new float3( 1,  0,  0),  // 18 - belongs to right
                    new float3( 0,  1,  0),  // 19 - belongs to up
                    new float3( 0,  0, -1),  // 20 - belongs to front

                    // right, up, back vertex
                    new float3( 1,  0,  0),  // 21 - belongs to right
                    new float3( 0,  1,  0),  // 22 - belongs to up
                    new float3( 0,  0,  1),  // 23 - belongs to back
                },
                Triangles = new ushort[]
                {
                   0,  6,  3,     3,  6,  9, // left
                   2, 14, 20,     2, 20,  8, // front
                  12, 15, 18,    15, 21, 18, // right
                   5, 11, 17,    17, 11, 23, // back
                   7, 22, 10,     7, 19, 22, // top
                   1,  4, 16,     1, 16, 13, // bottom 
                },
            };

            var shader = RC.CreateShader(_vertexShader, _pixelShader);
            RC.SetShader(shader);
            _xformParam = RC.GetShaderParam(shader, "xform");
            _xform = float4x4.Identity;

            // Set the clear color for the backbuffer
            RC.ClearColor = new float4(0.1f, 0.3f, 0.2f, 1);
        }

        static float4x4 ModelXForm(float3 pos, float3 rot, float3 pivot)
        {
            return float4x4.CreateTranslation(pos + pivot)
                   *float4x4.CreateRotationY(rot.y)
                   *float4x4.CreateRotationX(rot.x)
                   *float4x4.CreateRotationZ(rot.z)
                   *float4x4.CreateTranslation(-pivot);
        }

        // RenderAFrame is called once a frame
        public override void RenderAFrame()
        {
            // Clear the backbuffer
            RC.Clear(ClearFlags.Color | ClearFlags.Depth);

            float2 speed = Mouse.Velocity + Touch.GetVelocity(TouchPoints.Touchpoint_0);
            if (Mouse.LeftButton || Touch.GetTouchActive(TouchPoints.Touchpoint_0))
            {
                _alpha -= speed.x*0.0001f;
                _beta  -= speed.y*0.0001f;
            }

            _yawCube1 += Keyboard.ADAxis * 0.1f;
            _pitchCube1 += Keyboard.WSAxis * 0.1f;
            _yawCube2 += Keyboard.LeftRightAxis * 0.1f;
            _pitchCube2 += Keyboard.UpDownAxis * 0.1f;

            // Setup matrices
            var aspectRatio = Width / (float)Height;
            var projection = float4x4.CreatePerspectiveFieldOfView(3.141592f * 0.25f, aspectRatio, 0.01f, 20);
            var view = float4x4.CreateTranslation(0, 0, 3)*float4x4.CreateRotationY(_alpha)*float4x4.CreateRotationX(_beta);

            // First cube
            var cube1Model = ModelXForm(new float3(-0.5f, 0, 0), new float3(_pitchCube1, _yawCube1, 0), new float3(0, 0, 0));
            _xform = projection * view * cube1Model * float4x4.CreateScale(0.5f, 0.1f, 0.1f);
            RC.SetShaderParam(_xformParam, _xform);
            RC.Render(_mesh);

            // Second cube
            var cube2Model = ModelXForm(new float3(1, 0, 0), new float3(_pitchCube2, _yawCube2, 0), new float3(-0.5f, 0, 0));
            _xform = projection * view * cube1Model * cube2Model * float4x4.CreateScale(0.5f, 0.1f, 0.1f);
            RC.SetShaderParam(_xformParam, _xform);
            RC.Render(_mesh);

            // Swap buffers: Show the contents of the backbuffer (containing the currently rendered farame) on the front buffer.
            Present();
        }


        // Is called when the window was resized
        public override void Resize()
        {
            // Set the new rendering area to the entire new windows size
            RC.Viewport(0, 0, Width, Height);

            // Create a new projection matrix generating undistorted images on the new aspect ratio.
            var aspectRatio = Width/(float) Height;

            // 0.25*PI Rad -> 45° Opening angle along the vertical direction. Horizontal opening angle is calculated based on the aspect ratio
            // Front clipping happens at 1 (Objects nearer than 1 world unit get clipped)
            // Back clipping happens at 2000 (Anything further away from the camera than 2000 world units gets clipped, polygons will be cut)
            var projection = float4x4.CreatePerspectiveFieldOfView(3.141592f * 0.25f, aspectRatio, 1, 20000);
            RC.Projection = projection;
        }

    }
}