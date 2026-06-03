using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LoadingSceneManager : MonoBehaviour
{
    [Header("UI")]
    public Slider slider;

    public TextMeshProUGUI percentText;

    float progress;

    IEnumerator Start()
    {
        progress = 0f;

        // ================= WAIT RUNNER =================

        while (
            MatchmakingManager.Instance == null ||
            MatchmakingManager.Instance.runner == null
        )
        {
            yield return null;
        }

        // ================= LOADING LOOP =================

        while (true)
        {
            // scene chưa load xong
            if (!MatchmakingManager.SceneLoaded)
            {
                // chạy tới 90%
                progress += Time.deltaTime * 0.3f;

                progress =
                    Mathf.Clamp(
                        progress,
                        0f,
                        0.9f
                    );
            }
            else
            {
                // scene xong => chạy nốt 100%
                progress += Time.deltaTime * 1.5f;

                progress =
                    Mathf.Clamp01(progress);
            }

            // update UI
            if (slider != null)
            {
                slider.value = progress;
            }

            if (percentText != null)
            {
                percentText.text =
                    Mathf.RoundToInt(
                        progress * 100f
                    ) + "%";
            }

            // hoàn tất
            if (progress >= 1f)
            {
                break;
            }

            yield return null;
        }

        // ================= DONE =================

        yield return new WaitForSeconds(0.2f);

        Debug.Log("Loading Complete");

        gameObject.SetActive(false);
    }
}