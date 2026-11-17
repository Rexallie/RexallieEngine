Shader "Hidden/Rexallie/CharacterShadowBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend One Zero

        CGINCLUDE
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float4 _MainTex_TexelSize;
        float4 _SourceRect;
        float4 _SpriteUV;
        float _BlurStep;

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
            o.uv = v.uv;
            return o;
        }
        ENDCG

        // Pass 0: Extract white silhouette of sprite into padded _SourceRect
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragSilhouette

            fixed4 fragSilhouette(v2f i) : SV_Target
            {
                float2 rel = (i.uv - _SourceRect.xy) / _SourceRect.zw;
                if (rel.x < 0.0 || rel.x > 1.0 || rel.y < 0.0 || rel.y > 1.0)
                {
                    return fixed4(1.0, 1.0, 1.0, 0.0);
                }

                float2 spriteUv = _SpriteUV.xy + rel * _SpriteUV.zw;
                fixed alpha = tex2D(_MainTex, spriteUv).a;
                return fixed4(1.0, 1.0, 1.0, alpha);
            }
            ENDCG
        }

        // Pass 1: Horizontal 9-tap Gaussian blur
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragBlurH

            fixed4 fragBlurH(v2f i) : SV_Target
            {
                float2 stepSize = float2(_MainTex_TexelSize.x * _BlurStep, 0.0);
                float alpha = 0.0;
                alpha += tex2D(_MainTex, i.uv - stepSize * 4.0).a * 0.01621622;
                alpha += tex2D(_MainTex, i.uv - stepSize * 3.0).a * 0.05405405;
                alpha += tex2D(_MainTex, i.uv - stepSize * 2.0).a * 0.12162162;
                alpha += tex2D(_MainTex, i.uv - stepSize * 1.0).a * 0.19459459;
                alpha += tex2D(_MainTex, i.uv).a                  * 0.22702703;
                alpha += tex2D(_MainTex, i.uv + stepSize * 1.0).a * 0.19459459;
                alpha += tex2D(_MainTex, i.uv + stepSize * 2.0).a * 0.12162162;
                alpha += tex2D(_MainTex, i.uv + stepSize * 3.0).a * 0.05405405;
                alpha += tex2D(_MainTex, i.uv + stepSize * 4.0).a * 0.01621622;
                return fixed4(1.0, 1.0, 1.0, alpha);
            }
            ENDCG
        }

        // Pass 2: Vertical 9-tap Gaussian blur
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragBlurV

            fixed4 fragBlurV(v2f i) : SV_Target
            {
                float2 stepSize = float2(0.0, _MainTex_TexelSize.y * _BlurStep);
                float alpha = 0.0;
                alpha += tex2D(_MainTex, i.uv - stepSize * 4.0).a * 0.01621622;
                alpha += tex2D(_MainTex, i.uv - stepSize * 3.0).a * 0.05405405;
                alpha += tex2D(_MainTex, i.uv - stepSize * 2.0).a * 0.12162162;
                alpha += tex2D(_MainTex, i.uv - stepSize * 1.0).a * 0.19459459;
                alpha += tex2D(_MainTex, i.uv).a                  * 0.22702703;
                alpha += tex2D(_MainTex, i.uv + stepSize * 1.0).a * 0.19459459;
                alpha += tex2D(_MainTex, i.uv + stepSize * 2.0).a * 0.12162162;
                alpha += tex2D(_MainTex, i.uv + stepSize * 3.0).a * 0.05405405;
                alpha += tex2D(_MainTex, i.uv + stepSize * 4.0).a * 0.01621622;
                return fixed4(1.0, 1.0, 1.0, alpha);
            }
            ENDCG
        }
    }
}
