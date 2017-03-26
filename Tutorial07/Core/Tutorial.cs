﻿using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Fusee.Base.Common;
using Fusee.Base.Core;
using Fusee.Engine.Common;
using Fusee.Engine.Core;
using Fusee.Math.Core;
using Fusee.Serialization;
using Fusee.Xene;
using static System.Math;
using static Fusee.Engine.Core.Input;
using static Fusee.Engine.Core.Time;

namespace Fusee.Tutorial.Core
{
    class Picker : SceneVisitor
    {
        public float4x4 VP;
        public void SetViewport(float x, float y, float width, float height)
        {
            _vpOrigin.x = x;
            _vpOrigin.y = y;
            _vpSize.x = width;
            _vpSize.y = height;
        }

        public float2 Clip2Screen(float2 clip)
        {
            float2 tmp = new float2(clip.x, -clip.y);
            return (tmp + float2.One) * new float2(_vpSize.x / 2.0f, _vpSize.y / 2.0f) + _vpOrigin;
        }
        public float2 Screen2Clip(float2 screen)
        {
            float2 tmp = (screen - _vpOrigin) * new float2(2.0f / _vpSize.x, 2.0f / _vpSize.y) - float2.One;
            return new float2(tmp.x, -tmp.y);
        }

        public float2 PickPoint // internally keep the pickpoint in clip coordinates ([-1..1] along screen x and y)
        {
            set { _pickPoint = Screen2Clip(value);}
            get { return Clip2Screen(_pickPoint); }
        }

        public Picker()
        {
        }

        public class PickResult
        {
            public SceneNodeContainer Node;
            public float3 PickPntClip;
        }

        public IEnumerable<PickResult> PickResults => _pickResults;

        private CollapsingStateStack<float4x4> _model = new CollapsingStateStack<float4x4>();
        private List<PickResult> _pickResults = new List<PickResult>();
        private float2 _vpOrigin;
        private float2 _vpSize;
        private float2 _pickPoint;
        private float4x4 MVP;


        protected override void InitState()
        {
            _pickResults.Clear();
            _model.Clear();
            _model.Tos = float4x4.Identity;
        }
        protected override void PushState()
        {
            _model.Push();
        }
        protected override void PopState()
        {
            _model.Pop();
            MVP = VP * _model.Tos;
        }

        [VisitMethod]
        void OnMesh(MeshComponent mesh)
        {
            foreach (var tri in MeshTriangles(mesh))
            {
                float s, t;
                if (PointInTriangle(_pickPoint, tri, out s, out t))
                {
                    _pickResults.Add(new PickResult
                    {
                        Node = CurrentNode,
                        PickPntClip = new float3(_pickPoint.x, _pickPoint.y, 
                                tri.p0.z * s + tri.p1.z * t + tri.p2.z * (1.0f - s - t))
                    });
                }
            }
        }

        // Taken from Glenn Slayden's post on http://stackoverflow.com/questions/2049582/how-to-determine-a-point-in-a-2d-triangle
        private static bool PointInTriangle(float2 p, Tri tri, out float s, out float t)
        {
            s = tri.p0.y * tri.p2.x - tri.p0.x * tri.p2.y + (tri.p2.y - tri.p0.y) * p.x + (tri.p0.x - tri.p2.x) * p.y;
            t = tri.p0.x * tri.p1.y - tri.p0.y * tri.p1.x + (tri.p0.y - tri.p1.y) * p.x + (tri.p1.x - tri.p0.x) * p.y;

            if ((s < 0) != (t < 0))
                return false;

            var A = -tri.p1.y * tri.p2.x + tri.p0.y * (tri.p2.x - tri.p1.x) + tri.p0.x * (tri.p1.y - tri.p2.y) + tri.p1.x * tri.p2.y;
            if (A < 0.0)
            {
                s = -s;
                t = -t;
                A = -A;
            }
            return s > 0 && t > 0 && (s + t) <= A;
        }


        static bool PointClipped(float3 p)
        {
            return !((-1.0f <= p.x && p.x <= 1.0f) && (-1.0f <= p.y && p.y <= 1.0f) && (-1.0f <= p.z && p.z <= 1.0f));
        }

        private struct Tri
        {
            public float3 p0;
            public float3 p1;
            public float3 p2;
        }

        private IEnumerable<Tri> MeshTriangles(MeshComponent mesh)
        {
            if (mesh.Triangles == null || mesh.Triangles.Length == 0)
                yield break;

            float3[] verts = new float3[mesh.Vertices.Length];

            // transform all vertices to clip space
            for (int i = 0; i < verts.Length; i++)
            {
                float4 vClip = MVP*new float4(mesh.Vertices[i], 1.0f);
                vClip = vClip/vClip.z;
                verts[i] = vClip.xyz;
            }

            for (int i = 0; i < mesh.Triangles.Length; i += 3)
            {
                var tri = new Tri
                {
                    p0 = verts[mesh.Triangles[i]],
                    p1 = verts[mesh.Triangles[i + 1]],
                    p2 = verts[mesh.Triangles[i + 2]],
                };
                if (PointClipped(tri.p0) && PointClipped(tri.p1) && PointClipped(tri.p2))
                    continue;
                yield return tri;
            }
        }

        [VisitMethod]
        void OnTransform(TransformComponent xform)
        {
            _model.Tos *= xform.Matrix();
            MVP = VP * _model.Tos;
        }
    }


    class Renderer : SceneVisitor
    {
        public ShaderEffect ShaderEffect;

        public RenderContext RC;
        private ITexture _leafTexture;
        public float4x4 View;
        private Dictionary<MeshComponent, Mesh> _meshes = new Dictionary<MeshComponent, Mesh>();
        private CollapsingStateStack<float4x4> _model = new CollapsingStateStack<float4x4>();
        private Mesh LookupMesh(MeshComponent mc)
        {
            Mesh mesh;
            if (!_meshes.TryGetValue(mc, out mesh))
            {
                mesh = new Mesh
                {
                    Vertices = mc.Vertices,
                    Normals = mc.Normals,
                    UVs = mc.UVs,
                    Triangles = mc.Triangles,
                };
                _meshes[mc] = mesh;
            }
            return mesh;
        }

        public Renderer(RenderContext rc)
        {
            RC = rc;
            // Read the Leaves.jpg image and upload it to the GPU
            ImageData leaves = AssetStorage.Get<ImageData>("Leaves.jpg");
            _leafTexture = RC.CreateTexture(leaves);

            // Initialize the shader(s)
            ShaderEffect = new ShaderEffect(

                new[]
                {
                    new EffectPassDeclaration
                    {
                        VS = AssetStorage.Get<string>("VertexShader.vert"),
                        PS = AssetStorage.Get<string>("PixelShader.frag"),
                        StateSet = new RenderStateSet
                        {
                            ZFunc = Compare.Less
                            //ZEnable = true,
                            // CullMode = Cull.Clockwise,
                        }
                    }
                },
                new[]
                {
                    new EffectParameterDeclaration {Name = "albedo", Value = float3.One},
                    new EffectParameterDeclaration {Name = "shininess", Value = 1.0f},
                    new EffectParameterDeclaration {Name = "specfactor", Value= 1.0f},
                    new EffectParameterDeclaration {Name = "speccolor", Value = float3.Zero},
                    new EffectParameterDeclaration {Name = "ambientcolor", Value = float3.Zero},
                    new EffectParameterDeclaration {Name = "texture", Value = _leafTexture},
                    new EffectParameterDeclaration {Name = "texmix", Value = 0.0f},
                });
            ShaderEffect.AttachToContext(RC);
        }

        protected override void InitState()
        {
            _model.Clear();
            _model.Tos = float4x4.Identity;
        }
        protected override void PushState()
        {
            _model.Push();
        }
        protected override void PopState()
        {
            _model.Pop();
            RC.ModelView = View*_model.Tos;
        }
        [VisitMethod]
        void OnMesh(MeshComponent mesh)
        {
            ShaderEffect.RenderMesh(LookupMesh(mesh));
            // RC.Render(LookupMesh(mesh));
        }
        [VisitMethod]
        void OnMaterial(MaterialComponent material)
        {
            if (material.HasDiffuse)
            {
                ShaderEffect.SetEffectParam("albedo", material.Diffuse.Color);
                if (material.Diffuse.Texture == "Leaves.jpg")
                {
                    ShaderEffect.SetEffectParam("texture", _leafTexture);
                    ShaderEffect.SetEffectParam("texmix", 1.0f);
                }
                else
                {
                    ShaderEffect.SetEffectParam("texmix", 0.0f);
                }
            }
            else
            {
                ShaderEffect.SetEffectParam("albedo", float3.Zero);
            }
            if (material.HasSpecular)
            {
                ShaderEffect.SetEffectParam("shininess", material.Specular.Shininess);
                ShaderEffect.SetEffectParam("specfactor", material.Specular.Intensity);
                ShaderEffect.SetEffectParam("speccolor", material.Specular.Color);
            }
            else
            {
                ShaderEffect.SetEffectParam("shininess", 0);
                ShaderEffect.SetEffectParam("specfactor", 0);
                ShaderEffect.SetEffectParam("speccolor", float3.Zero);
            }
            if (material.HasEmissive)
            {
                ShaderEffect.SetEffectParam("ambientcolor", material.Emissive.Color);
            }
            else
            {
                ShaderEffect.SetEffectParam("ambientcolor", float3.Zero);
            }
        }
        [VisitMethod]
        void OnTransform(TransformComponent xform)
        {
            _model.Tos *= xform.Matrix();
            RC.ModelView = View * _model.Tos;
        }
    }


    [FuseeApplication(Name = "Tutorial Example", Description = "The official FUSEE Tutorial.")]
    public class Tutorial : RenderCanvas
    {
        // angle variables
        private static float _angleHorz = M.PiOver6 * 2.0f, _angleVert = -M.PiOver6 * 0.5f,
                             _angleVelHorz, _angleVelVert, _angleRoll, _angleRollInit, _zoomVel, _zoom;
        private static float2 _offset;
        private static float2 _offsetInit;

        private const float RotationSpeed = 7;
        private const float Damping = 0.8f;

        private SceneContainer _scene;
        private SceneContainer _screen;
        private float4x4 _sceneCenter;
        private float4x4 _sceneScale;
        private float4x4 _projection;
        private bool _twoTouchRepeated;

        private bool _keys;

        private TransformComponent _wuggyTransform;
        private TransformComponent _wgyWheelBigR;
        private TransformComponent _wgyWheelBigL;
        private TransformComponent _wgyWheelSmallR;
        private TransformComponent _wgyWheelSmallL;
        private TransformComponent _wgyNeckHi;
        private List<SceneNodeContainer> _trees;

        private Renderer _renderer;
        private Picker _picker;


        // Init is called on startup. 
        public override void Init()
        {
            // Load the scene
            _scene = AssetStorage.Get<SceneContainer>("WuggyLand.fus");
            _sceneScale = float4x4.CreateScale(0.04f);

            _screen = AssetStorage.Get<SceneContainer>("Screen.fus");


            // Instantiate our self-written renderer
            _renderer = new Renderer(RC);
            _picker = new Picker();

            // Find some transform nodes we want to manipulate in the scene
            _wuggyTransform = _scene.Children.FindNodes(c => c.Name == "Wuggy").First()?.GetTransform();
            _wgyWheelBigR = _scene.Children.FindNodes(c => c.Name == "WheelBigR").First()?.GetTransform();
            _wgyWheelBigL = _scene.Children.FindNodes(c => c.Name == "WheelBigL").First()?.GetTransform();
            _wgyWheelSmallR = _scene.Children.FindNodes(c => c.Name == "WheelSmallR").First()?.GetTransform();
            _wgyWheelSmallL = _scene.Children.FindNodes(c => c.Name == "WheelSmallL").First()?.GetTransform();
            _wgyNeckHi = _scene.Children.FindNodes(c => c.Name == "NeckHi").First()?.GetTransform();

            // Find the trees and store them in a list
            _trees = new List<SceneNodeContainer>();
            _trees.AddRange(_scene.Children.FindNodes(c => c.Name.Contains("Tree")));

            // Set the clear color for the backbuffer
            RC.ClearColor = new float4(1, 1, 1, 1);
        }

        // RenderAFrame is called once a frame
        public override void RenderAFrame()
        {
            bool doPick = false;
            // Clear the backbuffer
            RC.Clear(ClearFlags.Color | ClearFlags.Depth);

            // Mouse and keyboard movement
            if (Keyboard.LeftRightAxis != 0 || Keyboard.UpDownAxis != 0)
            {
                _keys = true;
            }

            var curDamp = (float)System.Math.Exp(-Damping * DeltaTime);

            // Zoom & Roll
            if (Touch.TwoPoint)
            {
                if (!_twoTouchRepeated)
                {
                    _twoTouchRepeated = true;
                    _angleRollInit = Touch.TwoPointAngle - _angleRoll;
                    _offsetInit = Touch.TwoPointMidPoint - _offset;
                }
                _zoomVel = Touch.TwoPointDistanceVel * -0.01f;
                _angleRoll = Touch.TwoPointAngle - _angleRollInit;
                _offset = Touch.TwoPointMidPoint - _offsetInit;
            }
            else
            {
                _twoTouchRepeated = false;
                _zoomVel = Mouse.WheelVel * -0.5f;
                _angleRoll *= curDamp * 0.8f;
                _offset *= curDamp * 0.8f;
            }

            // UpDown / LeftRight rotation
            if (Mouse.LeftButton)
            {
                _picker.PickPoint = Mouse.Position;
                doPick = true;
                _keys = false;
                _angleVelHorz = -RotationSpeed * Mouse.XVel * 0.000002f;
                _angleVelVert = -RotationSpeed * Mouse.YVel * 0.000002f;
            }
            else if (Touch.GetTouchActive(TouchPoints.Touchpoint_0) && !Touch.TwoPoint)
            {
                _keys = false;
                _picker.PickPoint = Touch.GetPosition(TouchPoints.Touchpoint_0);
                doPick = true;
                float2 touchVel;
                touchVel = Touch.GetVelocity(TouchPoints.Touchpoint_0);
                _angleVelHorz = -RotationSpeed * touchVel.x * 0.000002f;
                _angleVelVert = -RotationSpeed * touchVel.y * 0.000002f;
            }
            else
            {
                if (_keys)
                {
                    _angleVelHorz = -RotationSpeed * Keyboard.LeftRightAxis * 0.002f;
                    _angleVelVert = -RotationSpeed * Keyboard.UpDownAxis * 0.002f;
                }
                else
                {
                    _angleVelHorz *= curDamp;
                    _angleVelVert *= curDamp;
                }
            }

            float wuggyYawSpeed = Keyboard.WSAxis * Keyboard.ADAxis * 0.03f;
            float wuggySpeed = Keyboard.WSAxis * -10;

            // Wuggy XForm
            float wuggyYaw = _wuggyTransform.Rotation.y;
            wuggyYaw += wuggyYawSpeed;
            wuggyYaw = NormRot(wuggyYaw);
            float3 wuggyPos = _wuggyTransform.Translation;
            wuggyPos += new float3((float)Sin(wuggyYaw), 0, (float)Cos(wuggyYaw)) * wuggySpeed;
            _wuggyTransform.Rotation = new float3(0, wuggyYaw, 0);
            _wuggyTransform.Translation = wuggyPos;

            // Wuggy Wheels
            _wgyWheelBigR.Rotation += new float3(wuggySpeed * 0.008f, 0, 0);
            _wgyWheelBigL.Rotation += new float3(wuggySpeed * 0.008f, 0, 0);
            _wgyWheelSmallR.Rotation = new float3(_wgyWheelSmallR.Rotation.x + wuggySpeed * 0.016f, -Keyboard.ADAxis * 0.3f, 0);
            _wgyWheelSmallL.Rotation = new float3(_wgyWheelSmallR.Rotation.x + wuggySpeed * 0.016f, -Keyboard.ADAxis * 0.3f, 0);

            // SCRATCH:
            // _guiSubText.Text = target.Name + " " + target.GetComponent<TargetComponent>().ExtraInfo;
            SceneNodeContainer target = GetClosest();
            float camYaw = 0;
            if (target != null)
            {
                float3 delta = target.GetTransform().Translation - _wuggyTransform.Translation;
                camYaw = (float)Atan2(-delta.x, -delta.z) - _wuggyTransform.Rotation.y;
            }

            camYaw = NormRot(camYaw);
            float deltaAngle = camYaw - _wgyNeckHi.Rotation.y;
            if (deltaAngle > M.Pi)
                deltaAngle = deltaAngle - M.TwoPi;
            if (deltaAngle < -M.Pi)
                deltaAngle = deltaAngle + M.TwoPi; ;
            var newYaw = _wgyNeckHi.Rotation.y + (float)M.Clamp(deltaAngle, -0.06, 0.06);
            newYaw = NormRot(newYaw);
            _wgyNeckHi.Rotation = new float3(0, newYaw, 0);


            _zoom += _zoomVel;
            // Limit zoom
            if (_zoom < 80)
                _zoom = 80;
            if (_zoom > 2000)
                _zoom = 2000;

            _angleHorz += _angleVelHorz;
            // Wrap-around to keep _angleHorz between -PI and + PI
            _angleHorz = M.MinAngle(_angleHorz);

            _angleVert += _angleVelVert;
            // Limit pitch to the range between [-PI/2, + PI/2]
            _angleVert = M.Clamp(_angleVert, -M.PiOver2, M.PiOver2);

            // Wrap-around to keep _angleRoll between -PI and + PI
            _angleRoll = M.MinAngle(_angleRoll);


            // Create the camera matrix and set it as the current ModelView transformation
            var mtxRot = float4x4.CreateRotationZ(_angleRoll) * float4x4.CreateRotationX(_angleVert) * float4x4.CreateRotationY(_angleHorz);
            var mtxCam = float4x4.LookAt(0, 20, -_zoom, 0, 0, 0, 0, 1, 0);
            _renderer.View = mtxCam * mtxRot * _sceneScale;
            var mtxOffset = float4x4.CreateTranslation(2 * _offset.x / Width, -2 * _offset.y / Height, 0);
            RC.Projection = mtxOffset * _projection;

            
            _renderer.Traverse(_scene.Children);
            _renderer.Traverse(_screen.Children);
            if (doPick)
            {
                _picker.VP = RC.Projection*_renderer.View;
                _picker.Traverse(_screen.Children);
                string touchedOb = _picker.PickResults?.OrderBy(pr => pr.PickPntClip.z).FirstOrDefault()?.Node?.Name;
                if (!string.IsNullOrEmpty(touchedOb))
                {
                    Diagnostics.Log(touchedOb);
                }
            }


            // Swap buffers: Show the contents of the backbuffer (containing the currently rerndered farame) on the front buffer.
            Present();

        }

        private SceneNodeContainer GetClosest()
        {
            float minDist = float.MaxValue;
            SceneNodeContainer ret = null;
            foreach (var target in _trees)
            {
                var xf = target.GetTransform();
                float dist = (_wuggyTransform.Translation - xf.Translation).Length;
                if (dist < minDist && dist < 1000)
                {
                    ret = target;
                    minDist = dist;
                }
            }
            return ret;
        }

        public static float NormRot(float rot)
        {
            while (rot > M.Pi)
                rot -= M.TwoPi;
            while (rot < -M.Pi)
                rot += M.TwoPi;
            return rot;
        }



        // Is called when the window was resized
        public override void Resize()
        {
            // Set the new rendering area to the entire new windows size
            RC.Viewport(0, 0, Width, Height);
            _picker.SetViewport(0, 0, Width, Height);

            // Create a new projection matrix generating undistorted images on the new aspect ratio.
            var aspectRatio = Width / (float)Height;

            // 0.25*PI Rad -> 45° Opening angle along the vertical direction. Horizontal opening angle is calculated based on the aspect ratio
            // Front clipping happens at 1 (Objects nearer than 1 world unit get clipped)
            // Back clipping happens at 2000 (Anything further away from the camera than 2000 world units gets clipped, polygons will be cut)
            _projection = float4x4.CreatePerspectiveFieldOfView(M.PiOver4, aspectRatio, 1, 20000);
        }

    }
}