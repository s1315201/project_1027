Shader "NightMarket/Comic"
{
 Properties { _Color("Colour",Color)=(1,1,1,1) }
 SubShader {
 Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
 Pass {
 Tags { "LightMode"="UniversalForward" }
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 half4 _Color;
 CBUFFER_END
 struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
 struct Varyings {float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float3 viewWS:TEXCOORD1;};
 Varyings vert(Attributes v){Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.normalWS=TransformObjectToWorldNormal(v.normalOS);o.viewWS=GetWorldSpaceViewDir(TransformObjectToWorld(v.positionOS.xyz));return o;}
 half4 frag(Varyings i):SV_Target{
 float3 n=normalize(i.normalWS);float light=dot(n,normalize(float3(-.4,.8,.5)));
 half tone=light>.35?1:light>-.25?.8:.61;
 half edge=smoothstep(.12,.24,abs(dot(n,normalize(i.viewWS))));
 return half4(_Color.rgb*tone*lerp(.38,1,edge),1);
 }
 ENDHLSL
 }
 }
}
