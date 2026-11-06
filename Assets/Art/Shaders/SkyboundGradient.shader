Shader "Skybound/Gradient"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.1, 0.2, 0.5, 1)
        _BottomColor ("Bottom Color", Color) = (1, 0.5, 0.2, 1)
        _HorizonPower ("Horizon Power", Float) = 1.2
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" "PreviewType"="Plane" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            fixed4 _TopColor;
            fixed4 _BottomColor;
            float _HorizonPower;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = pow(saturate(i.uv.y), _HorizonPower);
                return lerp(_BottomColor, _TopColor, t);
            }
            ENDCG
        }
    }
}
