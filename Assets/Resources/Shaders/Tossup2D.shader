// The game's only shader: vertex colour times texture, alpha blended, no depth. _AlphaOnly = 1 draws a
// font atlas (Alpha8): the vertex colour with the glyph's coverage as alpha.
Shader "Hidden/Tossup2D"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _AlphaOnly ("Alpha only", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest Always
        Lighting Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _AlphaOnly;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 t = tex2D(_MainTex, i.uv);
                fixed4 font = fixed4(i.color.rgb, i.color.a * t.a);
                return lerp(t * i.color, font, step(0.5, _AlphaOnly));
            }
            ENDCG
        }
    }
}
