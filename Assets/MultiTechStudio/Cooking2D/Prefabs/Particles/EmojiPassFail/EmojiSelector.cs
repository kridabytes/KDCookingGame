using UnityEngine;

namespace MultiTechStudio.Cooking2D
{
    public class EmojiSelector : MonoBehaviour
    {
        public ParticleSystem emojiParticle;
        public Sprite[] loseEmoji;
        public Sprite[] winEmoji;
        public Material emojiMat;
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");

        public void SelectEmoji(bool win)
        {
            if (win)
            {
                int index = Random.Range(0, winEmoji.Length);
                emojiMat.SetTexture(MainTex, winEmoji[index].texture);
            }
            else
            {
                int index = Random.Range(0, loseEmoji.Length);
                emojiMat.SetTexture(MainTex, loseEmoji[index].texture);
            }

            emojiParticle.Play();
        }
    }
}