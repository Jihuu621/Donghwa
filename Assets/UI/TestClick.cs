using UnityEngine;

public class TestClick : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void click()
    {
        Debug.Log("Click");
        if (gameObject.name == "Form1Btn")
        {
            transform.parent.parent.Find("Form1").Find("Form1_S1").gameObject.SetActive(true);
            transform.parent.parent.Find("Form1").Find("Form1_S2").gameObject.SetActive(true);
            transform.parent.parent.Find("Form1").Find("Form1_S3").gameObject.SetActive(true);
            transform.parent.parent.Find("Form1").Find("Form1_S4").gameObject.SetActive(true);

            transform.parent.parent.Find("Form2").Find("Form2_S1").gameObject.SetActive(false);
            transform.parent.parent.Find("Form2").Find("Form2_S2").gameObject.SetActive(false);
            transform.parent.parent.Find("Form2").Find("Form2_S3").gameObject.SetActive(false);
            transform.parent.parent.Find("Form2").Find("Form2_S4").gameObject.SetActive(false);
        }
        if (gameObject.name == "Form2Btn")
        {
            transform.parent.parent.Find("Form1").Find("Form1_S1").gameObject.SetActive(false);
            transform.parent.parent.Find("Form1").Find("Form1_S2").gameObject.SetActive(false);
            transform.parent.parent.Find("Form1").Find("Form1_S3").gameObject.SetActive(false);
            transform.parent.parent.Find("Form1").Find("Form1_S4").gameObject.SetActive(false);

            transform.parent.parent.Find("Form2").Find("Form2_S1").gameObject.SetActive(true);
            transform.parent.parent.Find("Form2").Find("Form2_S2").gameObject.SetActive(true);
            transform.parent.parent.Find("Form2").Find("Form2_S3").gameObject.SetActive(true);
            transform.parent.parent.Find("Form2").Find("Form2_S4").gameObject.SetActive(true);
        }
    }
}
