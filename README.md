# Configurable Workflow Engine

This is a backend service that implements a configurable state-machine API, built for the Infonetica Software Engineer Intern take-home exercise.

The service allows clients to:
1.  Define workflows as a set of states and actions.
2.  Instantiate a defined workflow.
3.  Transition an instance between states by executing actions.
4.  List all definitions and running instances.

## Tech Stack

* **.NET 8 / C#**
* **ASP.NET Core Minimal API**

## How to Run the Project

1.  Ensure you have the **.NET 8 SDK** installed.
2.  Clone the repository.
3.  Open a terminal in the project's root directory.
4.  Run the application using the following command:
    ```bash
    dotnet run
    ```
5.  The API will be running on `http://localhost:<port>`.

## How to Test

Once the application is running, you can use the built-in Swagger UI to test all the API endpoints.

Navigate to **`http://localhost:<port>/swagger`** in your browser.

A sample JSON to define a simple "Document Approval" workflow is provided below. You can use this in the `/definitions` endpoint:

```json
{
  "id": "doc-approval",
  "states": [
    { "id": "draft", "isInitial": true, "isFinal": false, "enabled": true },
    { "id": "in-review", "isInitial": false, "isFinal": false, "enabled": true },
    { "id": "approved", "isInitial": false, "isFinal": true, "enabled": true },
    { "id": "rejected", "isInitial": false, "isFinal": true, "enabled": true }
  ],
  "actions": [
    { "id": "submit-for-review", "fromStates": ["draft"], "toState": "in-review", "enabled": true },
    { "id": "approve", "fromStates": ["in-review"], "toState": "approved", "enabled": true },
    { "id": "reject", "fromStates": ["in-review"], "toState": "rejected", "enabled": true }
  ]
}