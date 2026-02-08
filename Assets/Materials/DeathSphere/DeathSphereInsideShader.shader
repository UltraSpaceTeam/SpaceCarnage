Shader "Custom/DeathSphereInsideShader"
{
    Properties
    {
        _Color ("Fog Color", Color) = (0.7, 0.2, 1, 0.35)
        _EdgeStart ("Edge Start (0..1)", Range(0,1)) = 0.75
        _EdgeWidth ("Edge Width", Range(0.001,0.5)) = 0.12
        _Intensity ("Intensity", Range(0,3)) = 1.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }

        Cull Front
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _EdgeStart;
            float _EdgeWidth;
            float _Intensity;

            struct appdata { float4 vertex : POSITION; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 center = mul(unity_ObjectToWorld, float4(0,0,0,1)).xyz;

                float3 right = float3(unity_ObjectToWorld._m00, unity_ObjectToWorld._m10, unity_ObjectToWorld._m20);
                float radius = length(right) * 0.5;

                float dist = distance(i.worldPos, center);
                float r01 = saturate(dist / max(radius, 1e-5));

                float edge = smoothstep(_EdgeStart, _EdgeStart + _EdgeWidth, r01);

                fixed4 col = _Color;
                col.a *= edge * _Intensity;

                return col;
            }
            ENDCG
        }
    }
}
