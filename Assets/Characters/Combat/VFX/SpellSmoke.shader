Shader "GuerraDeEmpujes/SpellSmoke"
{
 Properties { _Color("Smoke",Color)=(.12,.10,.09,.35) }
 SubShader {
 Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
 Pass {
 Blend SrcAlpha OneMinusSrcAlpha
 ZWrite Off
 Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float4 _Color;
 CBUFFER_END
 struct A{float4 p:POSITION;float2 uv:TEXCOORD0;float4 c:COLOR;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 c:COLOR;};
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;o.c=a.c;return o;}
 half4 frag(V i):SV_Target{float2 p=(i.uv-.5)*2;float noise=.78+sin(i.uv.x*21+sin(i.uv.y*15))*sin(i.uv.y*24)*.22;float density=pow(saturate(1-length(p)),1.6)*noise;return half4(_Color.rgb*i.c.rgb,density*_Color.a*i.c.a);}
 ENDHLSL
 }
 }
}
