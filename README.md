# Initialisation
Make sure the Optitrack streaming client is set-up correctly, with the correct IP adresses. The Manus glove should be calibrated in the Manu Core application as well as loaded inside Unity.

# Calibration
Before starting the experiment, the pivot point of the glove needs to be calibrated. The reason of this, is that the geometric center of the markers on top of the gloves is not equal to the pivot point of the hand. This means that when the participant rotates their hand, the rotation of the virtual hand does not match. The image below shows this offset.

![plot](pivot_img.png)

In the current implementation, the geometric center of the trackers is determined by Optitrack, which is then sent to Unity. In Unity, this position is directly transferred as the position of a certain parent gameobject, which itself is empty. A virtual offset is artificially created by assigning a child gameobject to this parent gameobject. This child gameobject consists of the model, which the user can see. With this, we created our own pivot point in Unity. This is visualised in the image below.

![plot](unity_pivot.png)

However, the pivot point still needs to be calibrated. By positionally locking the to be calibrated object, and simply rotating it, the real pivot position can be found. With every rotation the object makes, the global position sent by optitrack can be saved. Finding the center of these global position will give the actual pivot point. A visualisation of this method can be found below.
![plot](cal_rotation.png)

Finding the center can be done through sphere fitting. A virtual sphere is made with all these global positions. In the current case, the exact solution is used as described in https://arxiv.org/abs/1506.02776. Now, having found the real pivot position, it is assigned to the child gameobject. This method assumes that the rotation of the glove is already calibrated.
## Code
In the Calibration.cs file the code for the calibration procedure can be found.
The following is called when the participant starts this calibration procedure. It calls the coroutine which handles the entire process.
```
    public void StartCalibration()
    {
        introductionPanel.SetActive(false);
        StartCoroutine(CalibrationHandler());
    }
```
The coroutine was made to repeat the calibration steps for each object. The logic is the same for each gameobject which needs to be calibrated; an example for one object can be found below. It sets the 'running' variable as true, which turns false once the calibration for a certain object is done at the end of the FourPointCalibration function. The checking for whether it becomes false is done with the 'WaitUntil' function. Afterwards, the coroutine continues with the next object, where it repeats these steps again
```
 IEnumerator CalibrationHandler()
    {
        running = true;
        //calibrate vr headset position
        StartCoroutine(FourPointCalibration(mainCamera,cameraPanels));
        //make sure code pauses untill calibration is finished
        yield return new WaitUntil(CanContinueCalibration);
        ...
    }
```
The coroutine below determines all the calibration points. A calibration point is the global position recorded when a specific action has been made by the user. These global positions can then be used for the sphere fitting algorithm in order to find the pivot point. The storing of a calibration points follows the same steps: 
1. First the instruction is shown, which tells the player which action to do
2. The function waits untill the button is pressed by the player
3. The global position of the calibration stepis then stored and the instruction is disabled. This is repeated untill all instructions have been shown, after which the origin of the object can be determined. The object which is being calibrated is then assigned with the new pivot point (the center of these calibration points).
(Note: Here,it is assumed that the amount of panels equals the amount of calibration points; since each different calibration step has a different instruction. However, for more accurate pivot calibration, this coroutine should be independent from the amount of instructions, so more calibration points can be stored, e.g. the points between actions. This should be improved in the future.)
```
  IEnumerator FourPointCalibration(GameObject calibratingObject, GameObject[] panels)
    {
        for (int i = 0; i < panels.Length; i++)
        {
            //show instructions specific panel
            panels[i].SetActive(true);
            //wait untill button is pressed
            pressed = false;
            yield return new WaitUntil(ContinueNextCalibrationPoint);
            //store calibration point
            calibrationPoints[i] = calibratingObject.transform.position;
            //close instruction
            panels[i].SetActive(false);
        }
        //find and replace origin of gameobject based on calibration results
        Vector3 newOrigin = FindOrigin();
        calibratingObject.transform.position = newOrigin;
        //activate gameobject
        calibratingObject.SetActive(true);
        running = false;
    }
```
The function below returns the center of the positions found through the calibration. This should be the 'real pivot point' and in global world coordinates. It implements the sphere fitting algorithm as described in https://arxiv.org/abs/1506.02776. The equations 26 through 34 are written in the function below, returning the center point.
```
     Vector3 FindOrigin()
     {
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
            d += N* Mathf.Pow(coordinate.x, 3) + N* coordinate.x* Mathf.Pow(coordinate.y,2)
                + N*coordinate.x* Mathf.Pow(coordinate.z, 2);
            
            f += -2 * N * Mathf.Pow(coordinate.y, 2);
            g += -2 * N * coordinate.y * coordinate.z;
            h += N * coordinate.y * Mathf.Pow(coordinate.x, 2)+ N * Mathf.Pow(coordinate.y, 3) 
                + N * coordinate.y * Mathf.Pow(coordinate.z, 2);
            l += -2*N * Mathf.Pow(coordinate.z, 2);
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
        g += 2*sum_y*sum_z;
        h += -sum_x_sq*sum_y - sum_y_sq*sum_y - sum_z_sq*sum_y;
        h = -h;
        j = c;
        k = g;
        l += 2 * Mathf.Pow(sum_z, 2);
        m += -sum_x_sq*sum_z - sum_y_sq*sum_z - sum_z_sq*sum_z;
        m = -m;
        nom = a*(f*l-g*k)-e*(b*l-c*k)+j*(b*g-c*f);
        x_0 = (d*(f*l-g*k)-h*(b*l-c*k) + m*(b*g-c*f)) / nom;
        y_0 = (a*(h*l-m*g)-e*(d*l-m*c) +j*(d*g-h*c))/ nom;
        z_0 = (a*(f*m-h*k)-e*(b*m-d*k) +j*(b*h-d*f))/ nom;
        
        return new Vector3(x_0,y_0,z_0);
      }
```
To test whether the sphere fitting works, a test case was developed here. Whatever the sphere fitting algorithm is, it should return (0,0,0)
```
    private void Start()
    {
        //test whether finding origin works
        calibrationPoints[0] = new Vector3(-2, 0, 0);
        calibrationPoints[1] = new Vector3(0, 2, 0);
        calibrationPoints[2] = new Vector3(0, 0, 2);
        calibrationPoints[3] = new Vector3(0, 0, -2);
        //should print (0,0,0)
        Debug.Log(FindOrigin());
    }
```
