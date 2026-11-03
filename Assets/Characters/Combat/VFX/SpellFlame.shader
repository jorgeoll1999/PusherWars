Shader "GuerraDeEmpujes/SpellFlame"
{
 Properties { [HDR]_Core("Hot core",Color)=(6,3,.35,1) [HDR]_Tail("Flame edge",Color)=(2.5,.09,.005,1) }
 SubShader {
 Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
 Pass {
 Blend SrcAlpha One
 ZWrite Off
 Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float4 _Core,_Tail;
 CBUFFER_END
 struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;return o;}
 half4 frag(V i):SV_Target{
 float t=i.uv.y;float x=i.uv.x*2-1;
 float ripple=sin(t*32-_Time.y*22+sin(x*13+_Time.y*8))*.09+sin(t*58-_Time.y*33)*.045;
 float width=pow(saturate(t),.32)*sqrt(saturate(1-pow((t-.48)/.52,2)));
 float edge=saturate((width-abs(x+ripple*(1-t)))*8);
 float center=pow(saturate(1-abs(x)*2.3),2)*smoothstep(.25,.85,t);
 float breakup=saturate(.7+sin(t*40-_Time.y*18+sin(x*9))* .3);
 return half4(lerp(_Tail.rgb,_Core.rgb,center),edge*breakup*smoothstep(0,.18,t));
 }
 ENDHLSL
 }
 }
}
