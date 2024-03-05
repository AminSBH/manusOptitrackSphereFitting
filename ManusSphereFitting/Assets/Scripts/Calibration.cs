using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manus.Skeletons
{
    public class Calibration : MonoBehaviour
    {
        public GameObject[] panels, helpers;
        public GameObject rightHand, endPanel;

        Vector3 realHandRotation;
        Vector3[] calibrationPoints = new Vector3[4] { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero };
        bool running, pressed;

        private void Awake()
        {
            realHandRotation = rightHand.transform.rotation.eulerAngles;
            rightHand.GetComponent<Skeleton>().enabled = false;

            StartCoroutine(CalibrationHandler());

        }

        /// <summary>
        /// Check for calibration handler to only continue when previous calibration is finished
        /// </summary>
        private bool CanContinueCalibration()
        {
            return !running;
        }
        /// <summary>
        /// When the instructor presses the button to confirm the calibration point, this returns true
        /// </summary>
        private bool ContinueNextCalibrationPoint()
        {
            return pressed;
        }

        /// <summary>
        /// Handles calibrations of all relevant gameobjects
        /// </summary>
        IEnumerator CalibrationHandler()
        {
            running = true;
            //calibrate right hand rotation
            StartCoroutine(RotationCalibration());
            yield return new WaitUntil(CanContinueCalibration);
            running = true;
            //calibrate right hand position
            StartCoroutine(CalibratePosition());
            yield return new WaitUntil(CanContinueCalibration);
            endPanel.SetActive(true);
        }
        /// <summary>
        /// The calibration of the rotation of a given object
        /// </summary>
        IEnumerator RotationCalibration()
        {
            panels[0].SetActive(true);
            helpers[0].SetActive(true);
            pressed = false;
            yield return new WaitUntil(ContinueNextCalibrationPoint);
            rightHand.transform.localEulerAngles = realHandRotation - rightHand.transform.rotation.eulerAngles;
            rightHand.GetComponent<Skeleton>().enabled = true;
            panels[0].SetActive(false);
            helpers[0].SetActive(false);
            running = false;
        }

        IEnumerator CalibratePosition()
        {
            //measure calirbation points
            for (int i = 1; i < panels.Length; i++)
            {
                //show instructions specific panel
                panels[i].SetActive(true);
                helpers[i].SetActive(true);
                //wait untill button is pressed
                pressed = false;
                yield return new WaitUntil(ContinueNextCalibrationPoint);
                //store calibration point
                calibrationPoints[i] = rightHand.transform.position;
                //close instruction
                panels[i].SetActive(false);
                helpers[i].SetActive(true);
            }
            //find and replace origin of gameobject based on calibration results
            Vector3 newOrigin = FindOrigin();
            rightHand.transform.position = newOrigin;
            running = false;
        }

        public void RestartCalibration()
        {
            endPanel.SetActive(false);
            StartCoroutine(CalibrationHandler());
        }

        public void EndCalibration()
        {
            endPanel.SetActive(false);
        }

        void Update()
        {
            //if enter is pressed, calibration point is confirmed
            if (Input.GetKeyDown(KeyCode.Return))
            {
                pressed = true;
            }
        }
        /// <summary>
        /// This function implements the sphere fitting algorithm as described in https://arxiv.org/abs/1506.02776.
        /// </summary>
        /// <returns>Vector3 - Center pivot gameobject</returns>
        Vector3 FindOrigin()
        {
            float sum_x = 0;
            float sum_y = 0;
            float sum_z = 0;
            float sum_x_sq = 0;
            float sum_y_sq = 0;
            float sum_z_sq = 0;
            float a = 0;
            float b = 0;
            float c = 0;
            float d = 0;
            float N = 0;
            float e = 0;
            float f = 0;
            float g = 0;
            float h = 0;
            float l = 0;
            float m = 0;
            float j = 0;
            float k = 0;
            float nom = 0;
            float x_0 = 0;
            float y_0 = 0;
            float z_0 = 0;

            foreach (Vector3 coordinate in calibrationPoints)
            {
                sum_x += coordinate.x;
                sum_y += coordinate.y;
                sum_z += coordinate.z;
                sum_x_sq += Mathf.Pow(coordinate.x, 2);
                sum_y_sq += Mathf.Pow(coordinate.y, 2);
                sum_z_sq += Mathf.Pow(coordinate.z, 2);
                a += -2 * N * Mathf.Pow(coordinate.x, 2);
                b += -2 * N * coordinate.x * coordinate.y;
                c += -2 * N * coordinate.x * coordinate.z;
                d += N * Mathf.Pow(coordinate.x, 3) + N * coordinate.x * Mathf.Pow(coordinate.y, 2)
                    + N * coordinate.x * Mathf.Pow(coordinate.z, 2);

                f += -2 * N * Mathf.Pow(coordinate.y, 2);
                g += -2 * N * coordinate.y * coordinate.z;
                h += N * coordinate.y * Mathf.Pow(coordinate.x, 2) + N * Mathf.Pow(coordinate.y, 3)
                    + N * coordinate.y * Mathf.Pow(coordinate.z, 2);
                l += -2 * N * Mathf.Pow(coordinate.z, 2);
                m += N * coordinate.z * Mathf.Pow(coordinate.x, 2) + N * Mathf.Pow(coordinate.z, 3)
                    + N * coordinate.z * Mathf.Pow(coordinate.y, 2);
            }
            a += 2 * Mathf.Pow(sum_x, 2);
            b += 2 * sum_x * sum_y;
            c += 2 * sum_x * sum_z;
            d += -sum_x_sq * sum_x - sum_y_sq * sum_x - sum_z_sq * sum_x;
            d = -d;

            e = b;
            f += 2 * Mathf.Pow(sum_y, 2);
            g += 2 * sum_y * sum_z;
            h += -sum_x_sq * sum_y - sum_y_sq * sum_y - sum_z_sq * sum_y;
            h = -h;
            j = c;
            k = g;
            l += 2 * Mathf.Pow(sum_z, 2);
            m += -sum_x_sq * sum_z - sum_y_sq * sum_z - sum_z_sq * sum_z;
            m = -m;
            nom = a * (f * l - g * k) - e * (b * l - c * k) + j * (b * g - c * f);
            x_0 = (d * (f * l - g * k) - h * (b * l - c * k) + m * (b * g - c * f)) / nom;
            y_0 = (a * (h * l - m * g) - e * (d * l - m * c) + j * (d * g - h * c)) / nom;
            z_0 = (a * (f * m - h * k) - e * (b * m - d * k) + j * (b * h - d * f)) / nom;

            return new Vector3(x_0, y_0, z_0);
        }
    }
}
