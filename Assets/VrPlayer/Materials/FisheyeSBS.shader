Shader "Custom/FisheyeSBS200" {
  Properties {
    _MainTex("SBS Fisheye Texture", 2D) =
        "black" {} _FOV("Field of View (degrees)", Float) = 200 _ProjectionType(
            "Projection (0=Equidistant,1=Stereographic,2=Equisolid,3=Orthographic)",
            Int) = 0 _CenterULeft("Left Center U", Float) =
            0.25 _CenterVLeft("Left Center V", Float) =
                0.5 _CenterURight("Right Center U", Float) = 0.75 _CenterVRight(
                    "Right Center V", Float) = 0.5 _RadiusU("Radius U", Float) =
                    0.25 _RadiusV("Radius V", Float) = 0.5
  } SubShader {
    Tags { "Queue" =
               "Background" "RenderType" =
                   "Background" "PreviewType" = "Skybox" } Cull Off ZWrite Off

        Pass {
      CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_instancing
#include "UnityCG.cginc"

      struct appdata {
        float4 vertex : POSITION;
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };

      struct v2f {
        float4 pos : SV_POSITION;
        float3 worldPos : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
      };

      sampler2D _MainTex;
      float4 _MainTex_ST;
      float _FOV;
      int _ProjectionType;
      float _CenterULeft, _CenterVLeft, _CenterURight, _CenterVRight;
      float _RadiusU, _RadiusV;

      v2f vert(appdata v) {
        v2f o;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_INITIALIZE_OUTPUT(v2f, o);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
        o.pos = UnityObjectToClipPos(v.vertex);
        o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
        return o;
      }

      // Преобразование угла θ в нормализованный радиус (0..1)
      float ProjectionRadius(float theta, float thetaMax) {
        float r = 0;
        if (_ProjectionType == 0)  // Equidistant (равноугольная)
        {
          r = theta / thetaMax;
        } else if (_ProjectionType == 1)  // Stereographic
        {
          r = tan(theta * 0.5) / tan(thetaMax * 0.5);
        } else if (_ProjectionType == 2)  // Equisolid angle
        {
          r = sin(theta * 0.5) / sin(thetaMax * 0.5);
        } else if (_ProjectionType == 3)  // Orthographic
        {
          r = sin(theta) / sin(thetaMax);
        }
        return r;
      }

      fixed4 frag(v2f i) : SV_Target {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

        // Индекс текущего глаза (0 – левый, 1 – правый)
        int eye = unity_StereoEyeIndex;

        // Определяем позицию глаза в мировом пространстве
        float3 camPos;
#if defined(UNITY_SINGLE_PASS_STEREO) || defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
        // Для single-pass режимов используем массив позиций глаз
        camPos = unity_StereoWorldSpaceCameraPos[eye];
#else
        // Для multi-pass или обычного рендера используем стандартную позицию
        // камеры
        camPos = _WorldSpaceCameraPos;
#endif

        // Направление из конкретного глаза к точке сферы
        float3 worldDir = normalize(i.worldPos - camPos);

        // Переводим в локальное пространство сферы (её ось +Z смотрит в центр
        // fisheye)
        float3 localDir =
            normalize(mul((float3x3)unity_WorldToObject, worldDir));

        // Угол от оптической оси (ось +Z)
        float cosTheta = clamp(localDir.z, -1.0, 1.0);
        float theta = acos(cosTheta);

        // Максимальный угол (половина FOV)
        float thetaMax = radians(_FOV) * 0.5;

        // За пределами FOV – чёрный цвет
        if (theta > thetaMax) {
          return fixed4(0, 0, 0, 1);
        }

        // Азимутальный угол в плоскости XY
        float phi = atan2(localDir.y, localDir.x);

        // Нормализованный радиус (0..1)
        float r = ProjectionRadius(theta, thetaMax);

        // Координаты внутри круга (диапазон -1..1)
        float dx = r * cos(phi);
        float dy = r * sin(phi);

        // Выбираем центр соответствующего fisheye-круга
        float centerU, centerV;
        if (eye == 0) {
          centerU = _CenterULeft;
          centerV = _CenterVLeft;
        } else {
          centerU = _CenterURight;
          centerV = _CenterVRight;
        }

        // Переводим координаты круга в UV
        float u = centerU + dx * _RadiusU;
        float v = centerV + dy * _RadiusV;

        // Сэмплируем текстуру
        fixed4 col = tex2D(_MainTex, float2(u, v));
        return col;
      }
      ENDCG
    }
  }
  FallBack Off
}