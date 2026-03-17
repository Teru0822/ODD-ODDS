Shader "Custom/CRTMonitor"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ScanlineColor ("Scanline Color", Color) = (0,0,0,1)
        _ScanlineCount ("Scanline Count", Float) = 150.0
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.3
        _VignetteIntensity ("Vignette Intensity", Range(0, 5)) = 1.2
        _Distortion ("Distortion (Curvature)", Range(0, 0.5)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            fixed4 _ScanlineColor;
            float _ScanlineCount;
            float _ScanlineIntensity;
            float _VignetteIntensity;
            float _Distortion;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // ブラウン管の湾曲（歪み）効果
            float2 curve(float2 uv)
            {
                uv = uv * 2.0 - 1.0;
                float2 offset = abs(uv.yx) / float2(4.0, 4.0);
                uv = uv + uv * offset * offset * _Distortion;
                uv = uv * 0.5 + 0.5;
                return uv;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. ブラウン管の画面の丸み（湾曲）
                float2 curvedUV = curve(i.uv);
                
                // UVが画面外になった部分は黒く塗りつぶす（はみ出し防止）
                if (curvedUV.x < 0.0 || curvedUV.x > 1.0 || curvedUV.y < 0.0 || curvedUV.y > 1.0)
                    return fixed4(0, 0, 0, 1);

                // 基本のカメラ映像を取得
                fixed4 col = tex2D(_MainTex, curvedUV);

                // 2. スキャンライン（横シマ模様）
                // sin波を使って画面のY座標に応じたシマ模様を作る
                float scanline = sin(curvedUV.y * _ScanlineCount * 3.14159);
                scanline = (scanline * 0.5 + 0.5) * _ScanlineIntensity;
                // 色を少し暗くしてシマを表現
                col.rgb = lerp(col.rgb, _ScanlineColor.rgb, scanline);

                // 3. ビネット（四隅を暗くする効果）
                float2 coord = curvedUV - 0.5;
                float vignette = 1.0 - dot(coord, coord) * _VignetteIntensity;
                col.rgb *= smoothstep(0.0, 1.0, vignette);

                return col;
            }
            ENDCG
        }
    }
}
