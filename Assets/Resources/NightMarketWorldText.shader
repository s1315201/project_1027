Shader "NightMarket/WorldText"
{
 Properties { _MainTex("Font atlas",2D)="white"{} _Color("Colour",Color)=(1,1,1,1) }
 SubShader {
 Tags {"Queue"="Transparent+20" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
 Pass {
 Tags {"LightMode"="SRPDefaultUnlit"}
 Blend SrcAlpha OneMinusSrcAlpha
 ZWrite Off
 ZTest LEqual
 Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
 CBUFFER_START(UnityPerMaterial)
 half4 _Color;
 CBUFFER_END
 struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
 struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
 Varyings vert(Attributes a){Varyings o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;o.color=a.color*_Color;return o;}
 half4 frag(Varyings i):SV_Target {half alpha=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a;return half4(i.color.rgb,i.color.a*alpha);}
 ENDHLSL
 }
 }
}
