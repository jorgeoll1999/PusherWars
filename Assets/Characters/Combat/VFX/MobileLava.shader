Shader "Guerra/Mobile Lava"
{
 Properties{_MainTex("Basalt and magma",2D)="white"{}}
 SubShader{Tags{"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
 Pass{HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma target 3.0
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
 struct A{float4 positionOS:POSITION;};struct V{float4 positionCS:SV_POSITION;float2 p:TEXCOORD0;};
 V vert(A i){V o;float3 w=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(w);o.p=w.xz;return o;}
 half4 frag(V i):SV_Target{
     float t = _Time.y;
     float2 uv = i.p * .075;
     half3 basalt = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv).rgb;
     half fissure = smoothstep(.075,.45,basalt.r - basalt.b);
     float2 ripple = float2(sin(uv.y*31.0-t*2.4),cos(uv.x*28.0+t*2.0));
     half3 warped = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv+ripple*.011*fissure).rgb;
     float2 current = uv*1.7 + float2(-.095,.048)*t + ripple*.035;
     half eddy = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,current).r;
     half crest = smoothstep(.18,.82,eddy);
     half3 magma = warped*(.56+crest*.8) + half3(.52,.26,.035)*crest;
     return half4(lerp(basalt*.68,magma,fissure),1);
 }
 ENDHLSL}}
}