Shader "Plasma/ParticleRender"
{
    Properties
    {
        _ParticleSize ("Particle Size", Float) = 0.05
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
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
            #pragma target 5.0
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            StructuredBuffer<float3> _PositionBuffer;
            StructuredBuffer<float4> _ColorBuffer;
            float _ParticleSize;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            v2f vert(appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                float3 particlePos = _PositionBuffer[instanceID];
                float4 particleColor = _ColorBuffer[instanceID];

                float size = _ParticleSize;
                float3 worldPos = particlePos + float3(v.vertex.xy * size, 0.0);

                o.pos = UnityObjectToClipPos(float4(worldPos, 1.0));
                o.uv = v.uv;
                o.color = particleColor;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 centerOffset = i.uv - 0.5;
                float distSq = dot(centerOffset, centerOffset);
                float radiusSq = 0.25;

                if (distSq > radiusSq) discard;

                float alpha = 1.0 - smoothstep(radiusSq * 0.6, radiusSq, distSq);
                fixed4 col = i.color;
                col.a *= alpha;

                float glow = exp(-distSq * 8.0);
                col.rgb += i.color.rgb * glow * 0.5;

                return col;
            }
            ENDCG
        }
    }
    Fallback "Standard"
}
