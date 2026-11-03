Shader "GuerraDeEmpujes/SpellEnergy"
{
 Properties { [HDR]_Color("Color",Color)=(1,.3,.02,1) _Opacity("Opacity",Range(0,1))=.7 _Style("Style",Float)=0 }
 SubShader {
 Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
 Pass {
 Blend SrcAlpha One
 ZWrite Off
 Cull Back
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float4 _Color; float _Opacity; float _Style;
 CBUFFER_END
 struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
 struct V {float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 view:TEXCOORD1;float3 local:TEXCOORD2;float2 uv:TEXCOORD3;};
 V vert(A a){V o;float3 p=TransformObjectToWorld(a.positionOS.xyz);o.pos=TransformWorldToHClip(p);o.normal=TransformObjectToWorldNormal(a.normalOS);o.view=GetWorldSpaceViewDir(p);o.local=a.positionOS.xyz;o.uv=a.uv;return o;}
 half4 frag(V i):SV_Target {
 float fres=pow(1-saturate(dot(normalize(i.normal),normalize(i.view))),2.4);
 float noise=sin(i.local.x*31+_Time.y*9+sin(i.local.z*23-_Time.y*6))*sin(i.local.y*29-_Time.y*12);
 float bands=pow(saturate(sin((i.uv.y*9+i.uv.x*3-_Time.y*1.8)*6.283)),10);
 float grid=pow(saturate(sin(i.uv.x*100+sin(i.uv.y*40))),24)*.3;
 float alpha=_Style>1.5 ? (fres*.8+bands*.12+grid*.12) : _Style>.5 ? (.14+fres*.55+bands*.6) : (.38+noise*.2+fres*.5);
 return half4(_Color.rgb*(1+bands*.5),saturate(alpha)*_Opacity*_Color.a);
 }
 ENDHLSL
 }
 }
}
