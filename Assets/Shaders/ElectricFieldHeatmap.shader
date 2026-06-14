Shader "Plasma/ElectricFieldHeatmap"
{
    Properties
    {
        _MainTex ("Field Texture", 2D) = "black" {}
        _Opacity ("Opacity", Range(0.0, 1.0)) = 0.5
        _FieldScale ("Field Scale", Float) = 10000000.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent-1"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Opacity;
            float _FieldScale;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                col.a *= _Opacity;
                return col;
            }
            ENDCG
        }
    }
    Fallback "Unlit/Transparent"
}
