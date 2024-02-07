## Projects

- Fiat Managment 
  - A tool that pulls data from a calendar which contains information about people who have driven the car and then calculates how much they owe or are owed, then sends them an invoice.
  - Techinal features: 
    - Integration with the nextcloud calendar API & UK fuel rates API.
    - Mathmatical operations to calculate distance driven, fuel used and cost of fuel.
    - Mathmatical operations to predict estimated fuel usage and cost for entries that have missing data.
    - Custom SQL operations that work via reflection to allow for easy database management (reduce the amount of SQL written).
- BSDataPuller 
  - A plugin for the game BeatSaber which exposes internal game statistics such as the players health, score, combo, etc. via an interface for other plugins to use as well as via a websocket server for other programs to use.
  - Techinal features: 
    - Method patching via HarmonyLib to expose internal game data.
    - Event listeners to expose internal game data.
    - Websocket server to expose internal game data.
    - Absctraction to reduce the amount of code needed for exposed data sets.
- Glyphy 
  - A tool that access the LED features on the Nothing Phone 1 which allows the user to create custom animations for the phones otherwise locked away rear LED matrix.
  - Techinal features: 
    - Writing data to device buffers to control the LED matrix.
- Java Coursework 
  - The aim of this coursework was to create a chat program that supported peer to peer messaging, host resolution and private relays.
  - Techinal features: 
    - Network session management.
    - Network host resolution.
    - Client authentication.
    - Custom messaging protocol (via object streams).
    - Thread synchronization.
- Java UI Parser 
  - This is a library that would parse XML files into Java UI components, the style of the XML files was based on C#'s XAML UI framework.
  - Techinal features: 
    - XML parsing.
    - Reflection to create UI components.
    - Dynamic component tree creation (the order in which components were added depended on their children and parents).
    - Abstraction and inheritance to reduce the amount of code needed to create the parser for each component type.
- CreateProcessAsUser 
  - A command line tool for windows that emulated the functionality of the sudo command on unix systems. This tool worked by having a service run as system which would communicate to the command line tool via a pipe which was then used to verify the user and their permissions, if valid for the requesated resource then the system process would create the elevated process by modifying the windows user token and hand it back to the command line tool.
  - Techinal features: 
    - IPC.
    - Windows user tokens.
    - Security permissions.
- CSharpTools 
  - Pipes & Shared Memory 
    - Two C# libraries that allowed for an easy setup of shared memory and IPC pipes between processes.
    - Techinal features: 
      - Data marshalling (converting data to bytes and back to allow support for IPC between languages not just C#).
  - Console extensions 
    - This library allowed for easier console app development by providing a simple interface for creating commands and arguments as well as I/O extensions for the console such as input int, asynchrnous output and log formatting.
    - Techinal features: 
      - Parrallel processing.
      - Reflection.
      - Custom attributes.
      - Thread synchronization.
- Algorithms Coursework 
  - The aim of this project was to create a program that would resolve the shortest path between two nodes.
  - Techinal features: 
    - Algorithms: 
      - Dijkstra's algorithm.
      - Bellman-Ford algorithm.
    - Graph data structure.
    - Graph visualisation.
    - Graph file parsing.
    - Graph file generation (via WebUI).
- BSDP-Overlay 
  - This project was a companion to the BSDataPuller project, it made use of the exposed data from the websocket server to allow users to create custom overlays via the web to display the exposed data from the game.
  - Techinal features: 
    - Websocket client.
    - WebUI.
    - Database storage.
    - Account management.
    - Content management.
- api-readie 
  - This private repository is the abckend of all of my web services, it handles all account managment, creation and deletion as well as custom database queries and wizards built for php to ease backend management.
  - Techinal features: 
    - Database management.
    - Security permissions.
- WebFileManager 
  - A php web-app that allows users to upload and share files on the web, it supported intergration to the google accounts api so that a user could share files to specific users instead of the whole web.
  - Techinal features: 
    - Database management.
    - Security permissions.
    - Dynamic frontend (one backend page creates many frontend pages).
    - Dynamic backend (content queried varies based on the logged in user).
    - Content management.
- Repo-App 
  - A windows desktop app that would serve as the hub for all of my desktop projects. It would keep programs up-to-date and launch them via reflection and finding a main method within the program dll.
  - Techinal features: 
    - Reflection.
    - Dynamic loading of dlls.
    - Content management.

## Project Similarities

The below have been ordered in most favoured topics.

1. Creation of tools to reduce the amount of code needed to create a specific type of program.
2. Dynamic runtime operations (reflection, dynamic loading, etc.).
3. Code optimisation via abstraction and inheritance.
4. Exposure of internal data via shared memory and network protocols.
5. User security and permissions.
6. Dynamic user interfaces.
7. Database management.

## Possible Project Ideas

1. A program to optimise written source code.
   - While compilers optimise code for runtime, this tools could be used to optimise source code to reduce the amount of code written by the developer and to promote better coding practices.
   - This project is probably the best suited for the coursework as the coursework would like to have a project that is unique and solves a problem. I will need to research into this topic to see if there are many tools like this, if not then it could be a very good project.
   - This project seems whithin a realistic scope for me, while it will be rather difficult I get the feeling that its a difficult enough challange for me but still within reason.
   - Keywords:
   - (Hint) Decompile source code for analysis as this could give a more consistent structure to work with.
2. A program to read binaries or memory to determine the structure of a program.
   - This could be used to determine the structure of a program to allow for easier reverse engineering, aka a decomplier.
   - This project could be a very good demonstration of the understanding of compiled codebases and proves as a useful tool for reverse engineering. However, I do know that there are many tools that exist for decompling binaries already so it is not as unique.