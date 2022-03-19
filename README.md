# RayTracing-with-Unity-URP
## Renderer Feature
We add a path tracing pass before postprocess pass by renderer feature, that's why we can run path tracing with Unity URP renderer.
To enable the raytracing feature, you should:
* Clone the scripts to your Unity-URP project assets (It must be Unity 2022.1+)
* Create URP Asset(with Universal Renderer) in your assets(by Create->Rendering->URP Asset)
* Unity will create two assets: "\*\*.asset" and "\*\*_Renderer.asset"
* Choose "\*\*_Renderer.asset", click "Add Renderer Feature" button at the bottom of the Inspector panel
* Choose "Ray Tracing Render Feature"
* Edit->Project Settings->Quality->Render Pipeline Asset, replace the default asset with the asset created before

Now, you have enabled the raytracing feature in your pipeline asset, the following steps tell you how to run a test scene with path tracing
* Choose your Volume(current camera under effect), add override "My Ray Tracing"
* **Enable all items and configure them**

A test scene may like this:
![图片](https://user-images.githubusercontent.com/33785908/157629119-299b1f9e-26db-41ce-8282-c1fbfa0e66db.png)
![image](https://user-images.githubusercontent.com/33785908/159110271-2d00e941-dc66-4817-b412-40dc553e7ada.png)
