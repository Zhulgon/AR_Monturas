using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void IrAInfo()
    {
        SceneManager.LoadScene("InfoScene");
    }

    public void IrAAR()
    {
        SceneManager.LoadScene("ARScene");
    }

    public void IrASplash()
    {
        SceneManager.LoadScene("SplashScene");
    }
}