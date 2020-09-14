Shader "Custom/Portal"
{
    Properties
    {
        _InactiveColor("Inactive Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off

        Pass {
            // Start our HLSL program
            CGPROGRAM
            #pragma vertex vert // compile function "vert" as vertex shader
            #pragma fragment frag // compile function "frag" as fragment shader
            #include "UnityCG.cginc" // Bring in predefined vars and helper functions

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            sampler2D _MainTex; // The RenderTexture we're getting from our camera!
            float4 _InactiveColor; 
            int displayMask; // set to 1 to display texture, otherwise will draw color

            // Vertex shader
            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex); // Transform the vertex from object space to camera clip space
                o.screenPos = ComputeScreenPos(o.vertex); // Compute texture coordinate for doing a screenspace-mapped texture sample
                return o;
            }
            // Fragment shader
            fixed4 frag(v2f i) : SV_Target{
                float2 uv = i.screenPos.xy / i.screenPos.w; // homogenize x and y coordinates
                fixed4 portalCol = tex2D(_MainTex, uv);
                return portalCol * displayMask + _InactiveColor * (1 - displayMask);
            }

            ENDCG
        }
    }
    FallBack "Standard" // for shadows
}
