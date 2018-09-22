# Building the Documentation with DocFX
* The docfx.exe tool must be installed on the Path.
  * Download from here: https://dotnet.github.io/docfx/.
* Build the Lmdb project.
* Execute BuildAndRun.cmd from the Docs directory.
## Publishing to GitHub
* On the first build, add the Docs/_site directory to the master branch and commit.
* Use `git subtree` to push to the gh-pages branch:\
  `git subtree push --prefix Docs/_site origin gh-pages`\
   where origin points to the GitHub repository

