# Async-Addressables-Manager
Asynchronously load multiple scenes using Unity's Addressables system.

## Prerequisites
This package uses the `UniTask` and `Scene Reference` packages. It is recommended to install these packages before installing this one.

1. https://github.com/Cysharp/UniTask
1. https://github.com/Jason-Skillman/Scene-Reference

---
**Note:**

Prerequisite packages can also be installed with the same steps below.

---

## How to install
This package can be installed through the Unity `Package Manager` with Unity version 2019.3 or greater.

Open up the package manager `Window/Package Manager` and click on `Add package from git URL...`.

![unity_package_manager_git_drop_down](Documentation~/images/unity_package_manager_git_drop_down.png)

Paste in this repository's url.

`https://github.com/Jason-Skillman/Scene-Fader-Manager.git`

![unity_package_manager_git_with_url](Documentation~/images/unity_package_manager_git_with_url.png)

Click `Add` and the package will be installed in your project.

---
**NOTE:** For Unity version 2019.2 or lower

If you are using Unity 2019.2 or lower than you will not be able to install the package with the above method. Here are a few other ways to install the package.
1. You can clone this git repository into your project's `Packages` folder.
1. Another alternative would be to download this package from GitHub as a zip file. Unzip and in the `Package Manager` click on `Add package from disk...` and select the package's root folder.

---

### Git submodule
Alternatively you can also install this package as a git submodule.

```console
$ git submodule add https://github.com/Jason-Skillman/Async-Addressables-Manager.git Packages/Async-Addressables-Manager
```

## Async Addressables Manager
Quick example of how to load multiple scenes using `AddressablesManager`.
```C#
string[] scenesToLoad =
{
    "Scene01",
    "Scene02",
    "Scene03",
};

await AddressablesManager.LoadScenesAsync(scenesToLoad, "LargeScene01");
```

Here is a list of functions you can use.
|Function|Description|
|---|---|
|`LoadScenesAsync`|Loads all scenes asynchronously using Unity Addressables.|
|`UnloadScenesAsync`|Unloads all scenes asynchronously using Unity Addressables.|
|`UnloadAllScenesExceptForAsync`|Unloads all scenes asynchronously except for scenesToKeep using Unity Addressables.|
|`LoadScenesBatchAsync`|Loads scenes, unloads scenes, and sets the active scene in one single batch.|
