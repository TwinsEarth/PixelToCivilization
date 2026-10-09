using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Slots;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class UniversalBlockFields
    {
        [GenerateBlocks("Universal Render Pipeline")]
        public struct SurfaceDescription
        {
            public static string name = "SurfaceDescription";
            public static BlockFieldDescriptor SpriteMask = new BlockFieldDescriptor(SurfaceDescription.name, "SpriteMask", "Sprite Mask", "SURFACEDESCRIPTION_SPRITEMASK",
                new ColorRGBAControl(new Color(1, 1, 1, 1)), ShaderStage.Fragment);

            public static BlockFieldDescriptor NormalAlpha = new BlockFieldDescriptor(SurfaceDescription.name, "NormalAlpha", "Normal Alpha", "SURFACEDESCRIPTION_NORMALALPHA",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor MAOSAlpha = new BlockFieldDescriptor(SurfaceDescription.name, "MAOSAlpha", "MAOS Alpha", "SURFACEDESCRIPTION_MAOSALPHA",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor Anisotropy = new BlockFieldDescriptor(SurfaceDescription.name, "Anisotropy", "Anisotropy", "SURFACEDESCRIPTION_ANISOTROPY",
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor Tangent = new BlockFieldDescriptor(SurfaceDescription.name, "Tangent", "Tangent", "SURFACEDESCRIPTION_TANGENT",
                new TangentControl(UnityEditor.ShaderGraph.Internal.CoordinateSpace.Tangent), ShaderStage.Fragment);
            public static BlockFieldDescriptor ClearCoatNormal = new BlockFieldDescriptor(SurfaceDescription.name, "ClearCoatNormal", "Clear Coat Normal", "SURFACEDESCRIPTION_CLEARCOATNORMAL",
                new NormalControl(UnityEditor.ShaderGraph.Internal.CoordinateSpace.Tangent), ShaderStage.Fragment);
            public static BlockFieldDescriptor ThinFilmThickness = new BlockFieldDescriptor(SurfaceDescription.name, "ThinFilmThickness", "Thin Film Thickness", "SURFACEDESCRIPTION_THINFILMTHICKNESS",
                new FloatControl(0.5f), ShaderStage.Fragment);
            public static BlockFieldDescriptor ThinFilmMask = new BlockFieldDescriptor(SurfaceDescription.name, "ThinFilmMask", "Thin Film Mask", "SURFACEDESCRIPTION_THINFILMMASK",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SlitsDistance = new BlockFieldDescriptor(SurfaceDescription.name, "SlitsDistance", "Slits Distance", "SURFACEDESCRIPTION_SLITSDISTANCE",
                new FloatControl(0.5f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SlitsMask = new BlockFieldDescriptor(SurfaceDescription.name, "SlitsMask", "Slits Mask", "SURFACEDESCRIPTION_SLITSMASK",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SlitsDirection = new BlockFieldDescriptor(SurfaceDescription.name, "SlitsDirection", "Slits Direction", "SURFACEDESCRIPTION_SLITSDIRECTION",
                new TangentControl(UnityEditor.ShaderGraph.Internal.CoordinateSpace.Tangent), ShaderStage.Fragment);
            public static BlockFieldDescriptor CustomIndirectSpecular = new BlockFieldDescriptor(SurfaceDescription.name, "IndirectSpecular", "Custom Indirect Specular", "SURFACEDESCRIPTION_INDIRECTSPECULAR",
                new Vector3Control(Vector3.zero), ShaderStage.Fragment);
            public static BlockFieldDescriptor IndirectSpecularMask = new BlockFieldDescriptor(SurfaceDescription.name, "IndirectSpecularMask", "Indirect Specular Mask", "SURFACEDESCRIPTION_INDIRECTSPECULARMASK",
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor CustomIndirectDiffuse = new BlockFieldDescriptor(SurfaceDescription.name, "IndirectDiffuse", "Custom Indirect Diffuse", "SURFACEDESCRIPTION_INDIRECTDIFFUSE",
                new Vector3Control(Vector3.zero), ShaderStage.Fragment);
            public static BlockFieldDescriptor IndirectDiffuseMask = new BlockFieldDescriptor(SurfaceDescription.name, "IndirectDiffuseMask", "Indirect Diffuse Mask", "SURFACEDESCRIPTION_INDIRECTDIFFUSEMASK",
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor CustomLighting = new BlockFieldDescriptor(SurfaceDescription.name, "CustomLighting", "Custom Lighting", "SURFACEDESCRIPTION_CUSTOMLIGHTING",
               new ColorControl(UnityEngine.Color.black, false), ShaderStage.Fragment);
            public static BlockFieldDescriptor CustomSpecularFDG = new BlockFieldDescriptor(SurfaceDescription.name, "CustomSpecularFDG", "Custom Specular FDG", "SURFACEDESCRIPTION_CUSTOMSPECULARFDG",
               new Vector3Control(Vector3.zero), ShaderStage.Fragment);
            public static BlockFieldDescriptor CustomEnergyCompensation = new BlockFieldDescriptor(SurfaceDescription.name, "CustomEnergyCompensation", "Custom Energy Compensation", "SURFACEDESCRIPTION_CUSTOMENERGYCOMPENSATION",
                new FloatControl(0.0f), ShaderStage.Fragment);
        }
    }
}
