using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Presentation.Battle.Characters
{
    public sealed class CubeBattleCharacterView : MonoBehaviour
    {
        private readonly List<Renderer> _renderers = new();
        private Color _baseColor;
        private Vector3 _homePosition;
        private Quaternion _homeRotation;
        private Coroutine _motion;

        public static CubeBattleCharacterView Create(Transform parent, string displayName, Color color)
        {
            var root = new GameObject($"{displayName} Cube Character");
            root.transform.SetParent(parent, false);
            var view = root.AddComponent<CubeBattleCharacterView>();
            view._baseColor = color;

            view.AddPart("Body", new Vector3(0f, 1.35f, 0f), new Vector3(0.8f, 1.2f, 0.5f));
            view.AddPart("Head", new Vector3(0f, 2.25f, 0f), new Vector3(0.65f, 0.65f, 0.65f));
            view.AddPart("Left Arm", new Vector3(-0.62f, 1.4f, 0f), new Vector3(0.3f, 1.05f, 0.3f));
            view.AddPart("Right Arm", new Vector3(0.62f, 1.4f, 0f), new Vector3(0.3f, 1.05f, 0.3f));
            view.AddPart("Left Leg", new Vector3(-0.23f, 0.4f, 0f), new Vector3(0.32f, 0.8f, 0.38f));
            view.AddPart("Right Leg", new Vector3(0.23f, 0.4f, 0f), new Vector3(0.32f, 0.8f, 0.38f));

            view._homePosition = root.transform.localPosition;
            view._homeRotation = root.transform.localRotation;
            return view;
        }

        public void PlayAttack(bool strong, float direction)
        {
            StartMotion(Lunge(strong ? 1.2f : 0.75f, direction, strong ? 0.34f : 0.24f));
        }

        public void PlayDefend()
        {
            StartMotion(Defend());
        }

        public void PlayHit(bool lethal, float direction)
        {
            StartMotion(Hit(lethal, direction));
        }

        private void AddPart(string partName, Vector3 localPosition, Vector3 localScale)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = partName;
            part.transform.SetParent(transform, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            var collider = part.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            var partRenderer = part.GetComponent<Renderer>();
            partRenderer.material.color = _baseColor;
            _renderers.Add(partRenderer);
        }

        private void StartMotion(IEnumerator routine)
        {
            if (_motion != null) StopCoroutine(_motion);
            transform.localPosition = _homePosition;
            transform.localRotation = _homeRotation;
            SetColor(_baseColor);
            _motion = StartCoroutine(routine);
        }

        private IEnumerator Lunge(float distance, float direction, float duration)
        {
            var target = _homePosition + Vector3.right * (distance * direction);
            yield return Move(_homePosition, target, duration * 0.45f);
            yield return Move(target, _homePosition, duration * 0.55f);
            _motion = null;
        }

        private IEnumerator Defend()
        {
            SetColor(new Color(0.3f, 0.9f, 1f));
            var startScale = transform.localScale;
            transform.localScale = new Vector3(startScale.x * 1.15f, startScale.y * 0.85f, startScale.z * 1.15f);
            yield return new WaitForSeconds(0.35f);
            transform.localScale = startScale;
            SetColor(_baseColor);
            _motion = null;
        }

        private IEnumerator Hit(bool lethal, float direction)
        {
            SetColor(Color.white);
            var start = _homePosition;
            var knockback = start + Vector3.right * (0.18f * direction);
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
                SetColor(_baseColor * 0.35f);
            }
            else
            {
                yield return Move(knockback, start, 0.12f);
                SetColor(_baseColor);
            }
            _motion = null;
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
            foreach (var item in _renderers)
                if (item != null) item.material.color = color;
        }

        private void OnDestroy()
        {
            foreach (var item in _renderers)
                if (item != null && item.material != null) Destroy(item.material);
        }
    }
}
