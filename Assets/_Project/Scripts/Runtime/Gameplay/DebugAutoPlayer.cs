#if UNITY_EDITOR || DEVELOPMENT_BUILD
using BlastPuzzle.Core;
using UnityEngine;

namespace BlastPuzzle.Gameplay
{
    public sealed class DebugAutoPlayer : MonoBehaviour
    {
        [SerializeField] private GameController game;
        [SerializeField] private float delayBetweenMoves = 0.15f;

        private readonly AutoPlayer _bot = new AutoPlayer(new SystemRandomSource(), 0.9f);
        private float _timer;

        public int MovesPlayed { get; private set; }

        public void Bind(GameController controller) => game = controller;

        private void Update()
        {
            if (game == null || !game.IsRunning || !game.AcceptsInput)
            {
                _timer = 0f;
                return;
            }

            _timer += Time.deltaTime;
            if (_timer < delayBetweenMoves) return;
            _timer = 0f;

            if (_bot.TryPickMove(game.Game, out GridPos pos))
            {
                game.TapCell(pos);
                MovesPlayed++;
            }
        }
    }
}
#endif
