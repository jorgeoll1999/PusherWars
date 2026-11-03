Shader "Guerra/Mobile Contact"
{
 SubShader{
 Tags{"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
 Pass{
 Blend SrcAlpha OneMinusSrcAlpha
 ZWrite Off
 Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A{float4 pos:POSITION;float2 uv:TEXCOORD0;};struct V{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
 V vert(A i){V o;o.pos=TransformObjectToHClip(i.pos.xyz);o.uv=i.uv*2-1;return o;}
 half4 frag(V i):SV_Target{half a=saturate(1-dot(i.uv,i.uv));return half4(.035,.025,.045,a*a*.55);}
 ENDHLSL
 }
 }
}
