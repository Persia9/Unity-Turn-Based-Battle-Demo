using System.Collections;
using UnityEngine;

namespace Presentation.Battle.Characters
{
    public sealed class CubeBattleCharacterView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private Color _defendColor;
        [SerializeField] private Color _hitColor;
        [SerializeField, Range(0f, 1f)] private float _defeatedBrightness;
        private Color[] _baseColors;
        private MaterialPropertyBlock _properties;
        private Vector3 _homeScale;
        private Vector3 _homePosition;
        private Quaternion _homeRotation;
        private Coroutine _motion;

        public void PlayAttack(bool strong, Vector3 worldDirection)
        {
            StartMotion(Lunge(strong ? 1.2f : 0.75f, ToLocalDirection(worldDirection),
                strong ? 0.34f : 0.24f));
        }

        public void PlayDefend()
        {
            StartMotion(Defend());
        }

        public void PlayHit(bool lethal, Vector3 worldDirection)
        {
            StartMotion(Hit(lethal, ToLocalDirection(worldDirection)));
        }

        private void Awake()
        {
            _homePosition = transform.localPosition;
            _homeRotation = transform.localRotation;
            _homeScale = transform.localScale;
            _properties = new MaterialPropertyBlock();
            _baseColors = new Color[_renderers.Length];
            for (var i = 0; i < _renderers.Length; i++)
            {
                var material = _renderers[i].sharedMaterial;
                _baseColors[i] = material.HasProperty(BaseColorId)
                    ? material.GetColor(BaseColorId)
                    : material.color;
            }
        }

        public void ResetPose()
        {
            if (_motion != null) StopCoroutine(_motion);
            _motion = null;
            transform.localPosition = _homePosition;
            transform.localRotation = _homeRotation;
            transform.localScale = _homeScale;
            RestoreColors();
        }

        private void StartMotion(IEnumerator routine)
        {
            ResetPose();
            _motion = StartCoroutine(routine);
        }

        private IEnumerator Lunge(float distance, Vector3 direction, float duration)
        {
            var target = _homePosition + direction * distance;
            yield return Move(_homePosition, target, duration * 0.45f);
            yield return Move(target, _homePosition, duration * 0.55f);
            _motion = null;
        }

        private IEnumerator Defend()
        {
            SetColor(_defendColor);
            var startScale = transform.localScale;
            transform.localScale = new Vector3(startScale.x * 1.15f, startScale.y * 0.85f, startScale.z * 1.15f);
            yield return new WaitForSeconds(0.35f);
            transform.localScale = startScale;
            RestoreColors();
            _motion = null;
        }

        private IEnumerator Hit(bool lethal, Vector3 direction)
        {
            SetColor(_hitColor);
            var start = _homePosition;
            var knockback = start + direction * 0.18f;
            yield return Move(start, knockback, 0.08f);
            if (lethal)
            {
                var from = transform.localRotation;
                var to = from * Quaternion.Euler(0f, 0f, -90f);
                for (var elapsed = 0f; elapsed < 0.35f; elapsed += Time.deltaTime)
                {
                    transform.localRotation = Quaternion.Slerp(from, to, elapsed / 0.35f);
                    yield return null;
                }

                transform.localRotation = to;
                RestoreColors(_defeatedBrightness);
            }
            else
            {
                yield return Move(knockback, start, 0.12f);
                RestoreColors();
            }

            _motion = null;
        }

        private Vector3 ToLocalDirection(Vector3 worldDirection)
        {
            var direction = transform.parent != null
                ? transform.parent.InverseTransformDirection(worldDirection)
                : worldDirection;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.right;
        }

        private IEnumerator Move(Vector3 from, Vector3 to, float duration)
        {
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                transform.localPosition = Vector3.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            transform.localPosition = to;
        }

        private void SetColor(Color color)
        {
            for (var i = 0; i < _renderers.Length; i++) ApplyColor(i, color);
        }

        private void RestoreColors(float brightness = 1f)
        {
            for (var i = 0; i < _renderers.Length; i++) ApplyColor(i, _baseColors[i] * brightness);
        }

        private void ApplyColor(int index, Color color)
        {
            var item = _renderers[index];
            if (item == null) return;
            item.GetPropertyBlock(_properties);
            _properties.SetColor(BaseColorId, color);
            _properties.SetColor(ColorId, color);
            item.SetPropertyBlock(_properties);
        }
    }
}