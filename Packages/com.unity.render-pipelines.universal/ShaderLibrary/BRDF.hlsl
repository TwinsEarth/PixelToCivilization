#ifndef UNIVERSAL_BRDF_INCLUDED
#define UNIVERSAL_BRDF_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/BSDF.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Deprecated.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

#define kDielectricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04) // standard dielectric reflectivity coef at incident angle (= 4%)

struct BRDFData
{
    half3 albedo;
    half3 diffuse;
    half3 specular;
    half reflectivity;
    half perceptualRoughness;
    half roughness;
    half roughness2;
    half grazingTerm;

    // We save some light invariant BRDF terms so we don't have to recompute
    // them in the light loop. Take a look at DirectBRDF function for detailed explaination.
    half normalizationTerm;     // roughness * 4.0 + 2.0
    half roughness2MinusOne;    // roughness^2 - 1.0


    #if _SPECULAR_MODEL_ANISO
    float roughnessT;
    float roughnessB;
    #endif
};

// Convert a roughness and an anisotropy factor into GGX alpha values respectively for the major and minor axis of the tangent frame
void GetAnisotropicRoughness(float Alpha, float Anisotropy, out float ax, out float ay)
{
    // Anisotropic parameters: ax and ay are the roughness along the tangent and bitangent	
    // Kulla 2017, "Revisiting Physically Based Shading at Imageworks"
    ax = Alpha * (1.0f + Anisotropy);
    ay = Alpha * (1.0f - Anisotropy);
}


half ReflectivitySpecular(half3 specular)
{
#if defined(SHADER_API_GLES)
    return specular.r; // Red channel - because most metals are either monochrome or with redish/yellowish tint
#else
    return Max3(specular.r, specular.g, specular.b);
#endif
}

half OneMinusReflectivityMetallic(half metallic)
{
    // We'll need oneMinusReflectivity, so
    //   1-reflectivity = 1-lerp(dielectricSpec, 1, metallic) = lerp(1-dielectricSpec, 0, metallic)
    // store (1-dielectricSpec) in kDielectricSpec.a, then
    //   1-reflectivity = lerp(alpha, 0, metallic) = alpha + metallic*(0 - alpha) =
    //                  = alpha - metallic * alpha
    half oneMinusDielectricSpec = kDielectricSpec.a;
    return oneMinusDielectricSpec - metallic * oneMinusDielectricSpec;
}

half MetallicFromReflectivity(half reflectivity)
{
    half oneMinusDielectricSpec = kDielectricSpec.a;
    return (reflectivity - kDielectricSpec.r) / oneMinusDielectricSpec;
}

inline void InitializeBRDFDataDirect(half3 albedo, half3 diffuse, half3 specular, half reflectivity, half oneMinusReflectivity, half smoothness, inout half alpha, out BRDFData outBRDFData)
{
    outBRDFData = (BRDFData)0;
    outBRDFData.albedo = albedo;
    outBRDFData.diffuse = diffuse;
    outBRDFData.specular = specular;
    outBRDFData.reflectivity = reflectivity;

    outBRDFData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(smoothness);
    outBRDFData.roughness           = max(PerceptualRoughnessToRoughness(outBRDFData.perceptualRoughness), HALF_MIN_SQRT);
    outBRDFData.roughness2          = max(outBRDFData.roughness * outBRDFData.roughness, HALF_MIN);
    outBRDFData.grazingTerm         = saturate(smoothness + reflectivity);
    outBRDFData.normalizationTerm   = outBRDFData.roughness * half(4.0) + half(2.0);
    outBRDFData.roughness2MinusOne  = outBRDFData.roughness2 - half(1.0);

    // Input is expected to be non-alpha-premultiplied while ROP is set to pre-multiplied blend.
    // We use input color for specular, but (pre-)multiply the diffuse with alpha to complete the standard alpha blend equation.
    // In shader: Cs' = Cs * As, in ROP: Cs' + Cd(1-As);
    // i.e. we only alpha blend the diffuse part to background (transmittance).
    #if defined(_ALPHAPREMULTIPLY_ON)
        // TODO: would be clearer to multiply this once to accumulated diffuse lighting at end instead of the surface property.
        outBRDFData.diffuse *= alpha;
    #endif
}

// Legacy: do not call, will not correctly initialize albedo property.
inline void InitializeBRDFDataDirect(half3 diffuse, half3 specular, half reflectivity, half oneMinusReflectivity, half smoothness, inout half alpha, out BRDFData outBRDFData)
{
    InitializeBRDFDataDirect(half3(0.0, 0.0, 0.0), diffuse, specular, reflectivity, oneMinusReflectivity, smoothness, alpha, outBRDFData);
}

// Initialize BRDFData for material, managing both specular and metallic setup using shader keyword _SPECULAR_SETUP.
inline void InitializeBRDFData(half3 albedo, half metallic, half3 specular, half smoothness, inout half alpha, out BRDFData outBRDFData)
{
#ifdef _SPECULAR_SETUP
    half reflectivity = ReflectivitySpecular(specular);
    half oneMinusReflectivity = half(1.0) - reflectivity;
    half3 brdfDiffuse = albedo * oneMinusReflectivity;
    half3 brdfSpecular = specular;
#else
    half oneMinusReflectivity = OneMinusReflectivityMetallic(metallic);
    half reflectivity = half(1.0) - oneMinusReflectivity;
    half3 brdfDiffuse = albedo * oneMinusReflectivity;
    half3 brdfSpecular = lerp(kDieletricSpec.rgb, albedo, metallic);
#endif

    InitializeBRDFDataDirect(albedo, brdfDiffuse, brdfSpecular, reflectivity, oneMinusReflectivity, smoothness, alpha, outBRDFData);
}

inline void InitializeBRDFData(inout SurfaceData surfaceData, out BRDFData brdfData)
{
    InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);
#if _SPECULAR_MODEL_ANISO
    #if _FABRIC_SILK
        ConvertAnisotropyToClampRoughness(brdfData.perceptualRoughness, surfaceData.anisotropy, brdfData.roughnessT, brdfData.roughnessB);
    #else
        GetAnisotropicRoughness(brdfData.roughness2, surfaceData.anisotropy, brdfData.roughnessT, brdfData.roughnessB);
    #endif
#endif
}

half3 ConvertF0ForClearCoat15(half3 f0)
{
    return ConvertF0ForAirInterfaceToF0ForClearCoat15Fast(f0);
}

inline void InitializeBRDFDataClearCoat(half clearCoatMask, half clearCoatSmoothness, inout BRDFData baseBRDFData, out BRDFData outBRDFData)
{
    outBRDFData = (BRDFData)0;
    outBRDFData.albedo = half(1.0);

    // Calculate Roughness of Clear Coat layer
    outBRDFData.diffuse             = kDielectricSpec.aaa; // 1 - kDielectricSpec
    outBRDFData.specular            = kDielectricSpec.rgb;
    outBRDFData.reflectivity        = kDielectricSpec.r;

    outBRDFData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(clearCoatSmoothness);
    outBRDFData.roughness           = max(PerceptualRoughnessToRoughness(outBRDFData.perceptualRoughness), HALF_MIN_SQRT);
    outBRDFData.roughness2          = max(outBRDFData.roughness * outBRDFData.roughness, HALF_MIN);
    outBRDFData.normalizationTerm   = outBRDFData.roughness * half(4.0) + half(2.0);
    outBRDFData.roughness2MinusOne  = outBRDFData.roughness2 - half(1.0);
    outBRDFData.grazingTerm         = saturate(clearCoatSmoothness + kDielectricSpec.x);

    // Modify Roughness of base layer using coat IOR
    half ieta                        = lerp(1.0h, CLEAR_COAT_IETA, clearCoatMask);
    half coatRoughnessScale          = Sq(ieta);
    half sigma                       = RoughnessToVariance(PerceptualRoughnessToRoughness(baseBRDFData.perceptualRoughness));

    baseBRDFData.perceptualRoughness = RoughnessToPerceptualRoughness(VarianceToRoughness(sigma * coatRoughnessScale));

    // Recompute base material for new roughness, previous computation should be eliminated by the compiler (as it's unused)
    baseBRDFData.roughness          = max(PerceptualRoughnessToRoughness(baseBRDFData.perceptualRoughness), HALF_MIN_SQRT);
    baseBRDFData.roughness2         = max(baseBRDFData.roughness * baseBRDFData.roughness, HALF_MIN);
    baseBRDFData.normalizationTerm  = baseBRDFData.roughness * 4.0h + 2.0h;
    baseBRDFData.roughness2MinusOne = baseBRDFData.roughness2 - 1.0h;

    // Darken/saturate base layer using coat to surface reflectance (vs. air to surface)
    baseBRDFData.specular = lerp(baseBRDFData.specular, ConvertF0ForClearCoat15(baseBRDFData.specular), clearCoatMask);
    // TODO: what about diffuse? at least in specular workflow diffuse should be recalculated as it directly depends on it.
}

BRDFData CreateClearCoatBRDFData(SurfaceData surfaceData, inout BRDFData brdfData)
{
    BRDFData brdfDataClearCoat = (BRDFData)0;

    #if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
    // base brdfData is modified here, rely on the compiler to eliminate dead computation by InitializeBRDFData()
    InitializeBRDFDataClearCoat(surfaceData.clearCoatMask, surfaceData.clearCoatSmoothness, brdfData, brdfDataClearCoat);
    #endif

    return brdfDataClearCoat;
}

// Computes the specular term for EnvironmentBRDF
half3 EnvironmentBRDFSpecular(BRDFData brdfData, half fresnelTerm)
{
    float surfaceReduction = 1.0 / (brdfData.roughness2 + 1.0);
    return half3(surfaceReduction * lerp(brdfData.specular, brdfData.grazingTerm, fresnelTerm));
}

half3 EnvironmentBRDF(BRDFData brdfData, half3 indirectDiffuse, half3 indirectSpecular, half fresnelTerm)
{
    half3 c = indirectDiffuse * brdfData.diffuse;
    c += indirectSpecular * EnvironmentBRDFSpecular(brdfData, fresnelTerm);
    return c;
}

half3 EnvironmentBRDF_Scalable(BRDFData brdfData, half3 indirectDiffuse, half3 indirectSpecular, half fresnelTerm
    #if (defined(_CLEARCOAT) || defined(_CLEARCOATMAP)) && defined(_SCALABLE_LIT)
    , half oneMinusCoatFresnel
    #endif
    #if defined(_THIN_FILM)
    , half3 iridescence, half thinFilmMask
    #endif
    #if defined(_DIFFRACTION_GRATINGS)
    , half3 diffractionGratings, half slitsMask
    #endif
    #if defined(_USE_PREINTEGRATED_FDG)
    , half3 specularFDG, half energyCompensation
    #endif
    )
{
    #if (defined(_CLEARCOAT) || defined(_CLEARCOATMAP)) && defined(_SCALABLE_LIT)
    indirectDiffuse *= oneMinusCoatFresnel;
    indirectSpecular *= oneMinusCoatFresnel * oneMinusCoatFresnel;
    #endif

    half3 iDiffuse = indirectDiffuse * brdfData.diffuse;
    #if defined(_USE_PREINTEGRATED_FDG)
    half3 envSpec = specularFDG * (1 + brdfData.specular * energyCompensation);
    #else
    half3 envSpec = EnvironmentBRDFSpecular(brdfData, fresnelTerm);
    #endif

    half3 iSpecular = indirectSpecular * envSpec;
    half3 c = iSpecular + iDiffuse;
    #if defined(_THIN_FILM)
    c += lerp(iSpecular, iridescence * envSpec, thinFilmMask);
    #endif
    #if defined(_DIFFRACTION_GRATINGS)
    c += diffractionGratings * iSpecular * slitsMask;
    #endif
    return c;
}


// Environment BRDF without diffuse for clear coat
half3 EnvironmentBRDFClearCoat(BRDFData brdfData, half clearCoatMask, half3 indirectSpecular, half fresnelTerm)
{
    float surfaceReduction = 1.0 / (brdfData.roughness2 + 1.0);
    return indirectSpecular * EnvironmentBRDFSpecular(brdfData, fresnelTerm) * clearCoatMask;
}

// Computes the scalar specular term for Minimalist CookTorrance BRDF
// NOTE: needs to be multiplied with reflectance f0, i.e. specular color to complete
half DirectBRDFSpecular(BRDFData brdfData, half3 normalWS, half3 lightDirectionWS, half3 viewDirectionWS)
{
    float3 lightDirectionWSFloat3 = float3(lightDirectionWS);
    float3 halfDir = SafeNormalize(lightDirectionWSFloat3 + float3(viewDirectionWS));

    float NoH = saturate(dot(float3(normalWS), halfDir));
    half LoH = half(saturate(dot(lightDirectionWSFloat3, halfDir)));

    // GGX Distribution multiplied by combined approximation of Visibility and Fresnel
    // BRDFspec = (D * V * F) / 4.0
    // D = roughness^2 / ( NoH^2 * (roughness^2 - 1) + 1 )^2
    // V * F = 1.0 / ( LoH^2 * (roughness + 0.5) )
    // See "Optimizing PBR for Mobile" from Siggraph 2015 moving mobile graphics course
    // https://community.arm.com/events/1155

    // Final BRDFspec = roughness^2 / ( NoH^2 * (roughness^2 - 1) + 1 )^2 * (LoH^2 * (roughness + 0.5) * 4.0)
    // We further optimize a few light invariant terms
    // brdfData.normalizationTerm = (roughness + 0.5) * 4.0 rewritten as roughness * 4.0 + 2.0 to a fit a MAD.
    float d = NoH * NoH * brdfData.roughness2MinusOne + 1.00001f;

    half LoH2 = LoH * LoH;
    half specularTerm = brdfData.roughness2 / ((d * d) * max(0.1h, LoH2) * brdfData.normalizationTerm);

    // On platforms where half actually means something, the denominator has a risk of overflow
    // clamp below was added specifically to "fix" that, but dx compiler (we convert bytecode to metal/gles)
    // sees that specularTerm have only non-negative terms, so it skips max(0,..) in clamp (leaving only min(100,...))
#if REAL_IS_HALF
    specularTerm = specularTerm - HALF_MIN;
    // Update: Conservative bump from 100.0 to 1000.0 to better match the full float specular look.
    // Roughly 65504.0 / 32*2 == 1023.5,
    // or HALF_MAX / ((mobile) MAX_VISIBLE_LIGHTS * 2),
    // to reserve half of the per light range for specular and half for diffuse + indirect + emissive.
    specularTerm = clamp(specularTerm, 0.0, 1000.0); // Prevent FP16 overflow on mobiles
#endif

    return specularTerm;
}
half DirectBRDFSpecular(BRDFData brdfData, float NoH, float LoH)
{
    float d = NoH * NoH * brdfData.roughness2MinusOne + 1.00001f;

    half LoH2 = LoH * LoH;
    half specularTerm = brdfData.roughness2 / ((d * d) * max(0.1h, LoH2) * brdfData.normalizationTerm);

#if REAL_IS_HALF
    specularTerm = specularTerm - HALF_MIN;
    specularTerm = clamp(specularTerm, 0.0, 1000.0);
#endif

    return specularTerm;
}


// Based on Minimalist CookTorrance BRDF
// Implementation is slightly different from original derivation: http://www.thetenthplanet.de/archives/255
//
// * NDF [Modified] GGX
// * Modified Kelemen and Szirmay-Kalos for Visibility term
// * Fresnel approximated with 1/LdotH
half3 DirectBDRF(BRDFData brdfData, half3 normalWS, half3 lightDirectionWS, half3 viewDirectionWS, bool specularHighlightsOff)
{
    // Can still do compile-time optimisation.
    // If no compile-time optimized, extra overhead if branch taken is around +2.5% on some untethered platforms, -10% if not taken.
    [branch] if (!specularHighlightsOff)
    {
        half specularTerm = DirectBRDFSpecular(brdfData, normalWS, lightDirectionWS, viewDirectionWS);
        half3 color = brdfData.diffuse + specularTerm * brdfData.specular;
        return color;
    }
    else
        return brdfData.diffuse;
}

// Based on Minimalist CookTorrance BRDF
// Implementation is slightly different from original derivation: http://www.thetenthplanet.de/archives/255
//
// * NDF [Modified] GGX
// * Modified Kelemen and Szirmay-Kalos for Visibility term
// * Fresnel approximated with 1/LdotH
half3 DirectBRDF(BRDFData brdfData, half3 normalWS, half3 lightDirectionWS, half3 viewDirectionWS)
{
#ifndef _SPECULARHIGHLIGHTS_OFF
    return brdfData.diffuse + DirectBRDFSpecular(brdfData, normalWS, lightDirectionWS, viewDirectionWS) * brdfData.specular;
#else
    return brdfData.diffuse;
#endif
}


static const half3 LUT[256] = 
{
    half3(0, 0, 0), half3(0, 0, 0), half3(0, 0, 0), half3(0, 0, 0), half3(0, 0, 0), half3(0, 0, 0), half3(0, 0, 0), half3(0, 0, 0), half3(0, 0, 0), half3(7.52413584272E-06, -4.95046457856E-06, 3.974999796312E-05), half3(0.0670127652928, -0.0495248797152, 0.4145760955852), half3(0.0826469869930002, -0.11869409759, 2.124109069111), half3(-0.6546492943783, 0.603027255402, 0.4337357867219), half3(-0.6763150864236, 1.394373961334, -0.1213121702302), half3(1.1261304840012, 1.048203716208, -0.1543821917596), half3(2.616305931117, 0.0584939412824002, -0.0605652665002), half3(0.9403974697005, -0.072342667162, -0.0100587649105), half3(0.08728398152445, -0.0079100244714, -0.00078367365665), half3(0.00496031287762435, -0.000448947176381213, -4.37550149716814E-05), half3(0.0004515078263345, -0.000120426008742, 0.0007475506323675), half3(0.0159886503495256, -0.0111898991096365, 0.091587025825653), half3(0.1373867975968, -0.1104478594056, 0.9645526169964), half3(0.1730303568821, -0.1763891737188, 2.0608970173831), half3(-0.0484937394595002, -0.0132495132964, 1.9114058962647), half3(-0.4789295133301, 0.388463606058, 0.8738443148673), half3(-0.7821221155359, 0.8228056829292, 0.1735208764731), half3(-0.8509964264056, 1.262407292826, -0.0637267629152), half3(-0.37688390461, 1.443227279658, -0.154681594534), half3(0.517147569347042, 1.26113967119942, -0.167912514784637), half3(1.73257825237475, 0.764638681560148, -0.128809125943834), half3(2.57755320276458, 0.24559147864524, -0.03200629540046), half3(2.4379393693918, -0.104639264748, 0.3732524042006), half3(1.6029046134487, -0.2292455608288, 1.2221707466573), half3(0.7525410143884, -0.2275629344568, 1.9486122212852), half3(0.25809154691845, -0.13441365243892, 2.12251128297751), half3(-0.0852745037600501, 0.0503446198054801, 1.75517295772281), half3(-0.412417716166815, 0.32619584932898, 1.05155152689375), half3(-0.652261927965388, 0.602812014552085, 0.433720448031655), half3(-0.814592522842148, 0.893408912984146, 0.12128670866865), half3(-0.866791495978123, 1.19814397795584, -0.0379193862231026), half3(-0.670109087607689, 1.39013637887241, -0.0868715098468134), half3(-0.229166691735445, 1.41465911662629, 0.0490480876061738), half3(0.4547086341856, 1.2252553470048, 0.5303471287838), half3(1.2950884919433, 0.9053427513528, 1.1490545760547), half3(2.0846470988309, 0.4981680971932, 1.7416719688327), half3(2.6312066995581, 0.1671896235436, 2.0557639241759), half3(2.63116402494103, -0.00735589547768565, 1.97285055572613), half3(2.10423542847114, -0.00337803780257855, 1.67783862382306), half3(1.22710921112133, 0.188505877111377, 1.15835475281032), half3(0.36995297856047, 0.42461371610784, 0.62252757925967), half3(-0.22842448284608, 0.65565029204516, 0.31191074218176), half3(-0.58766000412488, 0.88226856663632, 0.23339727608888), half3(-0.71593842441335, 1.0956171670294, 0.39020673781835), half3(-0.63325162834835, 1.24754539485828, 0.73058169437331), half3(-0.350929913041505, 1.27676062207229, 1.22378424903463), half3(-0.0311028104633755, 1.25942880561882, 1.65315115363643), half3(0.404539726954378, 1.16384332099627, 1.94238406206477), half3(0.895689660204006, 1.0477836988122, 1.96137244847213), half3(1.40892927816673, 0.884776029028058, 1.80269291741277), half3(1.83894525798048, 0.705475796478201, 1.55836764115038), half3(2.07657902549213, 0.632394676569631, 1.1685262084698), half3(2.13241474335693, 0.564209067032363, 0.788862002758453), half3(1.91382189003304, 0.565272554857509, 0.626738730561094), half3(1.49699485650823, 0.622711717996105, 0.760568109032139), half3(1.0212498305117, 0.734473245388, 1.0318359407939), half3(0.478520272867004, 0.894263453241766, 1.39647227157707), half3(0.0970658288506423, 1.06586899093822, 1.67849178582316), half3(-0.0770065851384404, 1.18117664753182, 1.93574215722452), half3(-0.072277408843897, 1.26678506001072, 1.9988959075907), half3(-0.015075471652025, 1.33075438372684, 1.91832951591884), half3(0.20687922529254, 1.32564230906096, 1.7645533338253), half3(0.48757940536848, 1.32075847690508, 1.52313700945312), half3(0.81363724257586, 1.28597555532126, 1.26799538615497), half3(1.21306716861471, 1.16319794171655, 1.15197733451075), half3(1.55815797799429, 1.05067402942089, 1.12749656964034), half3(1.85991853313689, 0.931372158488001, 1.34900502726469), half3(1.9978544063375, 0.877456058411462, 1.51836734279745), half3(1.96488091721106, 0.849303519960555, 1.77874058257257), half3(1.74981205566027, 0.91264816157328, 1.97230030679944), half3(1.41304811285872, 1.012993030755, 2.05113534009999), half3(1.10713098869913, 1.15184641121342, 2.01434254432894), half3(0.758177258220244, 1.25296398775701, 1.93615938892038), half3(0.498218349920957, 1.37512031525403, 1.80108047745522), half3(0.3543627050989, 1.44110599006599, 1.73593558784682), half3(0.341494546620182, 1.48778307378366, 1.65387023403119), half3(0.438618275226692, 1.51025548219258, 1.58876274874663), half3(0.695623222832097, 1.45132843441587, 1.68667100559727), half3(0.928789757242127, 1.40665796703784, 1.76669891066421), half3(1.25692028310235, 1.28650915804143, 1.92306940325813), half3(1.47353998859745, 1.25441176221339, 2.03863120304309), half3(1.72517561368173, 1.21484228477599, 2.09084877066427), half3(1.83699144681539, 1.2076730276865, 2.112664610744), half3(1.85505363712912, 1.23827051167042, 2.08188880022113), half3(1.78957533893561, 1.28180951630923, 2.04176760679624), half3(1.6549868662342, 1.32712663906951, 2.04791601098656), half3(1.4708235139643, 1.3875262003735, 2.04474335635084), half3(1.22619087035353, 1.48130864638748, 2.02045689844627), half3(1.06548830295143, 1.53326702041246, 2.04687486374647), half3(0.881174014493218, 1.55340425132229, 2.07247092510865), half3(0.857077132399679, 1.58219780776472, 2.0831244882105), half3(0.832125353411413, 1.59550315207342, 2.17292713842676), half3(0.960893846742351, 1.58379840691485, 2.21201257504622), half3(1.0481905918377, 1.59197479690144, 2.22970569742075), half3(1.23050958785822, 1.56724613078822, 2.23470984126927), half3(1.43249374201524, 1.54391945791093, 2.26102655060292), half3(1.60949923425004, 1.55111374323238, 2.31575832832421), half3(1.75582195322848, 1.55404468794566, 2.34745370475413), half3(1.83125889328332, 1.54533831429517, 2.36304873394498), half3(1.86753844870101, 1.53952687046699, 2.41915269457016), half3(1.79331492268944, 1.58461364335933, 2.34433974222876), half3(1.72931830940511, 1.61543161238305, 2.34799578059179), half3(1.60012304592336, 1.63901364988639, 2.37708465847869), half3(1.49657566151255, 1.68080471059969, 2.37736827129164), half3(1.3735788987569, 1.69578842312471, 2.42100308774486), half3(1.30729673868281, 1.73087451092201, 2.42282913164703), half3(1.18421591887705, 1.79522583134255, 2.46225514528185), half3(1.20251643276587, 1.80345047129142, 2.48884912588488), half3(1.30643887538725, 1.7826544095307, 2.60523606066135), half3(1.37179277563988, 1.80710473207184, 2.6233110862249), half3(1.50770061516648, 1.79993185913192, 2.7034372503095), half3(1.64418875887791, 1.82216606795859, 2.65920565213743), half3(1.76098834514626, 1.81279452278982, 2.64680501980279), half3(1.85461656956667, 1.80010676317359, 2.6336793772266), half3(1.92474343148709, 1.80788511425887, 2.61495883297298), half3(1.92762364836631, 1.83766124900082, 2.62602032009178), half3(1.90703135240977, 1.83162670268084, 2.6290738261969), half3(1.88059561803777, 1.83978595201493, 2.64802684868209), half3(1.79662858745868, 1.86949020884613, 2.75290088496), half3(1.70500636482888, 1.89578358923759, 2.80601236298672), half3(1.66991775179778, 1.91529834388404, 2.86897095599891), half3(1.56516333148436, 1.96977861300934, 2.87436639706086), half3(1.51952295526878, 1.99854932206975, 2.96265326577375), half3(1.57367166561026, 2.02320421322207, 2.93155553824191), half3(1.56525772065815, 2.0678624115283, 2.86783441728045), half3(1.64267025103089, 2.06521946031498, 2.87307964921737), half3(1.78365075162846, 2.04888337157495, 2.87854806529117), half3(1.87876311447913, 2.04425633740886, 2.90986639708517), half3(1.95355925753625, 2.05292746368255, 2.91860643476909), half3(2.05021344475023, 2.00753888928703, 2.98705967019484), half3(2.03351493163441, 2.03607452499368, 3.01835967368958), half3(2.06105321466162, 2.03036569262208, 3.08995878209154), half3(2.02037739147805, 2.05477456884202, 3.12139782095133), half3(1.99764997569374, 2.09953106209551, 3.14640615999526), half3(1.95585070153688, 2.13991428550371, 3.14417857705047), half3(1.87510965939139, 2.19260635342013, 3.16882706774683), half3(1.85134825008135, 2.22031835218865, 3.15478855218064), half3(1.84973236320307, 2.24566043736575, 3.15818898679866), half3(1.7962774089988, 2.25450433616884, 3.13099577047319), half3(1.86399096160832, 2.24705590319624, 3.1658651492241), half3(1.87778451339491, 2.26110009438279, 3.21181724326382), half3(1.94957388919883, 2.25922496418292, 3.20139339503454), half3(2.01843408474525, 2.26213164342099, 3.24427519215368), half3(2.10140461805153, 2.26158734382674, 3.29399456003499), half3(2.14560028055848, 2.2598588171356, 3.32133363562261), half3(2.15558981000462, 2.26128516665665, 3.38094425444701), half3(2.18445514797115, 2.29342949646435, 3.34707305633892), half3(2.13685073725715, 2.32984778699638, 3.39237864892821), half3(2.14132166419761, 2.35492361482416, 3.42537738305682), half3(2.07727797760293, 2.39956150930599, 3.39749561769489), half3(2.10181680740548, 2.42790160651974, 3.41533039176325), half3(2.11479139580284, 2.45707241683286, 3.38693694597893), half3(2.130923435018, 2.4479578298516, 3.44483444040596), half3(2.14049683644273, 2.42614984694231, 3.50161487730439), half3(2.13437035214701, 2.44392789665591, 3.4848275169367), half3(2.18079213712024, 2.43359458806326, 3.55050980263827), half3(2.16925220370227, 2.47506378993207, 3.55312145058805), half3(2.20629546215771, 2.47759194967607, 3.605670638589), half3(2.2421492684074, 2.49804408478006, 3.64008529950561), half3(2.25442681998686, 2.50931051909472, 3.66456131521719), half3(2.26489347893656, 2.53402755029957, 3.70713642718273), half3(2.31590811529965, 2.54419722636046, 3.72066022714571), half3(2.32445986072746, 2.56788465858282, 3.67542328708913), half3(2.32881169001323, 2.60636731556884, 3.71110862207915), half3(2.30004835762981, 2.63390262790032, 3.69440755485791), half3(2.39466340283119, 2.61082767371236, 3.7599326474103), half3(2.37291544098135, 2.62195925896257, 3.74381216280751), half3(2.40998829331872, 2.62437008669184, 3.75704886893421), half3(2.35135645100388, 2.65967716550071, 3.79952417994276), half3(2.42539612102967, 2.64534124249898, 3.88635097322368), half3(2.38185806943645, 2.66078014390639, 3.94150588973258), half3(2.39255701515236, 2.70416321942267, 3.91579523476491), half3(2.39649409770575, 2.71847858720632, 4.00693978470576), half3(2.41231756218252, 2.72732981879407, 3.95400886397897), half3(2.46010200788417, 2.71484233566702, 3.97448289864816), half3(2.44506391061688, 2.76317563636037, 3.92564561038496), half3(2.48157490175564, 2.7994199695155, 3.96104308107069), half3(2.48629362593498, 2.80827603038394, 3.96236746535666), half3(2.53273785440532, 2.8233814764718, 3.9620091658128), half3(2.55639676576011, 2.8048274805005, 4.07972847554449), half3(2.61108839893721, 2.80942361556169, 4.11559954375634), half3(2.57203996030324, 2.83795365506428, 4.11533325929723), half3(2.59460318964121, 2.83659168672732, 4.20305884710157), half3(2.61305364957601, 2.85984960943515, 4.22865641347057), half3(2.57694333116346, 2.88778002315619, 4.19577018253312), half3(2.56316068421794, 2.90103316987442, 4.26599307803029), half3(2.57088452125424, 2.942695276125, 4.18992416820259), half3(2.6166386054045, 2.92939440394707, 4.20286825925483), half3(2.64899793043747, 2.96006070031607, 4.20538274563474), half3(2.58800632308016, 2.98031290693901, 4.2258617067831), half3(2.63139814854806, 3.01121926026558, 4.23942736963358), half3(2.69885993543311, 3.00686495085333, 4.34692880952201), half3(2.73680822988791, 2.99569175659258, 4.37459674311118), half3(2.76979734755828, 3.00635550328037, 4.41276657629495), half3(2.74290418046984, 3.01668548640592, 4.47243516087088), half3(2.77712588969312, 3.03866497214696, 4.49806266887067), half3(2.78790576375802, 3.06252857069024, 4.46900728435977), half3(2.75698398140828, 3.0888230534156, 4.46279164179167), half3(2.74364634627837, 3.11275045358329, 4.46717958836319), half3(2.73250207301547, 3.15128014698915, 4.4206628749043), half3(2.79001755885236, 3.13801346484563, 4.46526183153206), half3(2.83572147175948, 3.12486435557719, 4.52528132372816), half3(2.83108050233902, 3.14747875512961, 4.58235224387869), half3(2.80615284415987, 3.19815543802928, 4.57218522165147), half3(2.8153770449589, 3.21464310426889, 4.57348652932075), half3(2.87122868798137, 3.19318784973897, 4.63325163352988), half3(2.91840506476964, 3.21450421981333, 4.68027214665719), half3(2.89281292403559, 3.22633011538718, 4.71886324747569), half3(2.93527625165713, 3.26581478315608, 4.67152523758381), half3(2.95364659998081, 3.28023913522999, 4.66714157823939), half3(2.91275476923346, 3.29877836782811, 4.68572576354178), half3(2.96473801256159, 3.28620108427548, 4.72076997393443), half3(2.92015283719931, 3.30143694717004, 4.7720907442882), half3(3.00116023991368, 3.31986303946769, 4.75886005650672), half3(2.98616795588141, 3.34013096381876, 4.76833931911287), half3(2.99117493900244, 3.35637759847781, 4.83007370219757), half3(3.01552195624679, 3.3704444917406, 4.8942143536508), half3(3.0397458222858, 3.38737500146275, 4.96115083110676), half3(3.07686862552851, 3.3783553999917, 4.98811281547055), half3(3.06737037761324, 3.40078511916599, 5.01273901822108), half3(3.06287681992202, 3.44763167655462, 4.94915639677229), half3(3.0406092973619, 3.45763452263932, 4.97245835903166), half3(3.12473430874803, 3.45899473244861, 5.00582991399243), half3(3.10948339113284, 3.49867099305431, 4.9270694116551), half3(3.14432102363114, 3.48325396202773, 4.97964443541386), half3(3.12293503870751, 3.5140448528081, 5.03224034102197), half3(3.123039934618, 3.54142627469588, 5.10053420183896), half3(3.18413361243242, 3.54179962915337, 5.06656429138882), half3(3.13824517157314, 3.55534161013859, 5.14330544988218), half3(3.2018454611706, 3.52875142568129, 5.21163658190135), half3(3.20730139478512, 3.560047905513, 5.22264643431017), half3(3.20986565991647, 3.57253020068308, 5.25531965359381), half3(3.24271390712047, 3.62964518945461, 5.21972966016645), half3(3.27915832849087, 3.62799809164332, 5.19702895665842), half3(3.24751209304952, 3.64294411336861, 5.25594592048978), half3(3.26159908985936, 3.66567382660009, 5.29285989834676), half3(3.30296924303627, 3.65356703373372, 5.23784173739679), half3(3.32309882830756, 3.67988421822301, 5.29701991750726), half3(3.31768972348906, 3.70266545159917, 5.33667809118637), half3(3.3484349471315, 3.70839948856374, 5.39868198965501), half3(3.33619903285989, 3.72510662858234, 5.38704829346129), half3(3.34370929548489, 3.74939131790496, 5.43170498101581), half3(3.32884580819251, 3.75765996767663, 5.47861837519744), half3(3.34875455125838, 3.77260426524078, 5.41753638378602), half3(3.38035371417671, 3.80346196945315, 5.45563895207479), half3(3.37283548821554, 3.82500084761243, 5.50470765511153), half3(3.43390244308697, 3.83479832884772, 5.43854679034097)
};

// Approximated diffraction grating
// not physically correct but simple to use
half3 DiffractionGrating(half3 lightDir, half3 viewDir,  half3 slitsDirection, half3 normalWS, half distance)
{

    half NdotL = (dot(normalWS, lightDir));
    half NdotV = (dot(normalWS, viewDir));
    if (NdotL <= 0 || NdotV <= 0)
        return 0;
    
    half SdotL = (dot(slitsDirection, lightDir));
    half SdotV = (dot(slitsDirection, viewDir));

    half sinDiff = SdotV - SdotL;
    half OPD = 0.5f * (distance * abs(sinDiff));
    half UV_L = floor(OPD * 255);
    half UV_R = ceil(OPD * 255);
    half weight = OPD * 255 - UV_L;
    half3 color = lerp(LUT[UV_L], LUT[UV_R], weight);

    return clamp(color, 0, 1);
}
#endif
