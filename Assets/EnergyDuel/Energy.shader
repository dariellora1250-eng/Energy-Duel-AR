Shader "EnergyDuel/Holograma"
{
    Properties { _Color ("Color", Color) = (0.1,0.85,1,1) _Opacity ("Opacidad", Range(0,1)) = 1 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            half _Opacity;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; half light : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.pos=UnityObjectToClipPos(v.vertex);
                o.light=.74h+.26h*abs(dot(UnityObjectToWorldNormal(v.normal),normalize(half3(.3,.55,-1))));
                return o;
            }
            fixed4 frag(v2f i) : SV_Target { return fixed4(_Color.rgb*i.light,_Color.a*_Opacity); }
            ENDCG
        }
    }
}
