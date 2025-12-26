# VS Code Agent Chat Export Format Specification

## Overview

The VS Code agent chat export is a JSON file (default filename: `chat.json`) that captures the complete state of a chat session between a user and a chat agent (like Copilot). This format is produced by the `workbench. action.chat.export` command and can be re-imported using `workbench.action.chat.import`.

### Source Files

This documentation is based on the following source files in the [microsoft/vscode](https://github.com/microsoft/vscode) repository:

- **Export/Import Logic**: [`src/vs/workbench/contrib/chat/browser/actions/chatImportExport.ts`](https://github.com/microsoft/vscode/blob/1debf21160174ecaf114e8e043146da08ba25d4a/src/vs/workbench/contrib/chat/browser/actions/chatImportExport.ts)
- **Data Models**: [`src/vs/workbench/contrib/chat/common/chatModel.ts`](https://github.com/microsoft/vscode/blob/1debf21160174ecaf114e8e043146da08ba25d4a/src/vs/workbench/contrib/chat/common/chatModel.ts)
- **Chat Service Types**: [`src/vs/workbench/contrib/chat/common/chatService.ts`](https://github.com/microsoft/vscode/blob/1debf21160174ecaf114e8e043146da08ba25d4a/src/vs/workbench/contrib/chat/common/chatService.ts)
- **Parser Types**: [`src/vs/workbench/contrib/chat/common/chatParserTypes.ts`](https://github.com/microsoft/vscode/blob/1debf21160174ecaf114e8e043146da08ba25d4a/src/vs/workbench/contrib/chat/common/chatParserTypes.ts)
- **Variable Entries**: [`src/vs/workbench/contrib/chat/common/chatVariableEntries.ts`](https://github.com/microsoft/vscode/blob/1debf21160174ecaf114e8e043146da08ba25d4a/src/vs/workbench/contrib/chat/common/chatVariableEntries. ts)
- **Agent Types**: [`src/vs/workbench/contrib/chat/common/chatAgents.ts`](https://github.com/microsoft/vscode/blob/1debf21160174ecaf114e8e043146da08ba25d4a/src/vs/workbench/contrib/chat/common/chatAgents.ts)

---

## Root Object Structure:  `IExportableChatData`

The exported JSON has the following top-level structure: 

```typescript
{
  "initialLocation": string | undefined,
  "responderUsername": string,
  "responderAvatarIconUri": ThemeIcon | UriComponents | undefined,
  "requests": ISerializableChatRequestData[]
}
```

**Source**: [`chatModel.ts` lines 1275-1280](https://github.com/microsoft/vscode/blob/1debf21160174ecaf114e8e043146da08ba25d4a/src/vs/workbench/contrib/chat/common/chatModel.ts#L1275-L1280)

### Fields

| Field | Type | Description |
|-------|------|-------------|
| `initialLocation` | `ChatAgentLocation` | Where the chat session was started.  Possible values: `"panel"`, `"editorInline"`, `"terminal"`, `"notebook"`, `"chat"` |
| `responderUsername` | `string` | The display name of the chat agent responding (e.g., `"Copilot"`, `"GitHub Copilot"`) |
| `responderAvatarIconUri` | `ThemeIcon \| UriComponents` | Icon for the responder - can be a theme icon ID (e.g., `{"id": "copilot"}`) or a URI to an image |
| `requests` | `array` | Array of all request/response exchanges in chronological order |

---

## Request Object Structure: `ISerializableChatRequestData`

Each item in the `requests` array represents a single user request and its corresponding AI response. 

**Source**: [`chatModel.ts` lines 1250-1268](https://github.com/microsoft/vscode/blob/1debf21160174ecaf114e8e043146da08ba25d4a/src/vs/workbench/contrib/chat/common/chatModel.ts#L1250-L1268)

```typescript
{
  // Request identification
  "requestId": string,

  // User's message
  "message": string | IParsedChatRequest,
  "variableData": IChatRequestVariableData,

  // Response data
  "response":  ResponseContent[] | undefined,
  "responseId": string | undefined,

  // Metadata
  "timestamp":  number | undefined,
  "agent": ISerializableChatAgentData | undefined,
  "slashCommand": IChatAgentCommand | undefined,
  "modelId": string | undefined,

  // Response state
  "result":  IChatAgentResult | undefined,
  "modelState": ResponseModelStateT | undefined,

  // User feedback
  "vote":  ChatAgentVoteDirection | undefined,
  "voteDownReason":  ChatAgentVoteDownReason | undefined,

  // Additional context
  "usedContext": IChatUsedContext | undefined,
  "contentReferences": IChatContentReference[] | undefined,
  "codeCitations": IChatCodeCitation[] | undefined,
  "followups": IChatFollowup[] | undefined,

  // Editing session data
  "confirmation": string | undefined,
  "editedFileEvents": IChatAgentEditedFileEvent[] | undefined,

  // Display state
  "shouldBeRemovedOnSend": IChatRequestDisablement | undefined,
  "isHidden": boolean | undefined,

  // Response timing
  "timeSpentWaiting": number | undefined,

  // Code block information
  "responseMarkdownInfo": ISerializableMarkdownInfo[] | undefined
}
```

### Field Descriptions

| Field | Description |
|-------|-------------|
| `requestId` | Unique identifier for this request (format: `"request_<uuid>"`) |
| `message` | The user's input message - either a plain string (older format) or a parsed request object |
| `variableData` | Context/attachments provided with the request |
| `response` | Array of response content parts from the AI |
| `responseId` | Unique identifier for the response (format: `"response_<uuid>"`) |
| `timestamp` | Unix timestamp in milliseconds when request was made |
| `agent` | Information about which agent handled the request |
| `slashCommand` | If a slash command was used (e.g., `/fix`, `/explain`) |
| `modelId` | ID of the language model used for this request |
| `result` | Final result/status of the response |
| `modelState` | State of the response (complete, cancelled, failed, etc.) |
| `vote` | User's feedback vote (0=down, 1=up) |
| `voteDownReason` | Reason provided if user voted down |
| `usedContext` | Context documents used by the agent |
| `contentReferences` | References cited in the response |
| `codeCitations` | Code attribution information |
| `followups` | Suggested follow-up questions |
| `confirmation` | Confirmation message if user confirmed an action |
| `editedFileEvents` | Files that were edited during this request |
| `shouldBeRemovedOnSend` | If request is hidden/disabled |
| `isHidden` | Legacy field for hidden state |
| `timeSpentWaiting` | Time spent waiting for user confirmation (milliseconds) |
| `responseMarkdownInfo` | Information about code blocks in the response |

---

## User Message:  `IParsedChatRequest`

The `message` field can be a simple string (older format) or a parsed request object. 

**Source**: [`chatParserTypes.ts` lines 19-32](https://github.com/microsoft/vscode/blob/1debf21160174ecaf114e8e043146da08ba25d4a/src/vs/workbench/contrib/chat/common/chatParserTypes.ts#L19-L32)

```typescript
{
  "text": string,
  "parts": IParsedChatRequestPart[]
}
```

| Field | Description |
|-------|-------------|
| `text` | The full raw text of the user's message |
| `parts` | Parsed components of the message |

### Message Parts (`IParsedChatRequestPart`)

Each part has a `kind` field indicating its type. 

**Source**: [`chatParserTypes. ts` lines 17-291](https://github.com/microsoft/vscode/blob/1debf21160174ecaf114e8e043146da08ba25d4a/src/vs/workbench/contrib/chat/common/chatParserTypes. ts#L17-L291)

| Kind | Description | Key Fields |
|------|-------------|------------|
| `"text"` | Plain text segment | `text` |
| `"agent"` | Agent mention (e.g., `@workspace`) | `agent` (agent data object) |
| `"subcommand"` | Slash command (e.g., `/fix`) | `command` |
| `"tool"` | Tool reference (e.g., `#codebase`) | `toolName`, `toolId` |
| `"toolset"` | Tool set reference | `name`, `tools` |
| `"dynamic"` | Dynamic variable reference (e.g., `#file: path`) | `text`, `id`, `data`, `modelDescription`, `isFile`, `isDirectory` |
| `"var"` | Legacy static variable (deprecated) | `variableName`, `variableArg` |
| `"slash"` | Standalone slash command | `slashCommand` |
| `"prompt"` | Prompt file reference | `name` |

Each part includes position information: 

```typescript
{
  "range": {
    "start": number,
    "endExclusive": number
  },
  "editorRange": {
    "startLineNumber": number,
    "startColumn": number,
    "endLineNumber": number,
    "endColumn": number
  }
}
```

---

## Variable Data:  `IChatRequestVariableData`

Contains context and attachments provided with the request.

**Source**: [`chatModel.ts` lines 54-56](https://github.com/microsoft/vscode/blob/1debf21160174ecaf114e8e043146da08ba25d4a/src/vs/workbench/contrib/chat/common/chatModel.ts#L54-L56)

```typescript
{
  "variables": IChatRequestVariableEntry[]
}
```

### Variable Entry Types

**Source**: [`chatVariableEntries.ts` lines 21-278](https://github.com/microsoft/vscode/blob/1debf21160174ecaf114e8e043146da08ba25d4a/src/vs/workbench/contrib/chat/common/chatVariableEntries.ts#L21-L278)

Each entry has a `kind` field:

| Kind | Description | Value Type |
|------|-------------|------------|
| `"file"` | Attached file | URI to the file |
| `"directory"` | Attached directory | URI to the directory |
| `"symbol"` | Code symbol reference | `Location` with URI and range |
| `"image"` | Attached image | Binary data or URI |
| `"tool"` | Attached tool | Tool configuration |
| `"toolset"` | Set of tools | Array of tool entries |
| `"implicit"` | Auto-detected context (e.g., current file) | URI or Location |
| `"paste"` | Pasted code | Code string with metadata |
| `"diagnostic"` | Error/warning reference | Diagnostic info |
| `"terminalCommand"` | Terminal command context | Command, output, exit code |
| `"generic"` | Generic context value | Various |
| `"workspace"` | Workspace context | Workspace info |
| `"string"` | String value | String content |
| `"promptFile"` | Prompt file reference | URI |
| `"promptText"` | Prompt text content | String |
| `"notebookOutput"` | Notebook output | Output data |
| `"element"` | UI element reference | Element data |
| `"scmHistoryItem"` | Source control history | SCM data |
| `"scmHistoryItemChange"` | SCM change | Change data |
| `"debugVariable"` | Debug variable | Expression and value |

### Common Fields for All Entries

```typescript
{
  "id": string,
  "name": string,
  "fullName": string | undefined,
  "kind": string,
  "value": any,
  "icon": ThemeIcon | undefined,
  "range": IOffsetRange | undefined,
  "modelDescription": string | undefined,
  "references": IChatContentReference[] | undefined,
  "omittedState": OmittedState | undefined
}
```

#### OmittedState Values

| Value | Meaning |
|-------|---------|
| `0` | `NotOmitted` - Full content included |
| `1` | `Partial` - Content was partially included |
| `2` | `Full` - Content was fully omitted |

---

## Response Content

The `response` field is an array of content parts that make up the AI's response.  Each part has a `kind` field. 

**Source**: [`chatService.ts` lines 107-751](https://github.com/microsoft/vscode/blob/1debf21160174ecaf114e8e043146da08ba25d4a/src/vs/workbench/contrib/chat/common/chatService.ts#L107-L751) and [`chatModel.ts` lines 119-166](https://github.com/microsoft/vscode/blob/1debf21160174ecaf114e8e043146da08ba25d4a/src/vs/workbench/contrib/chat/common/chatModel.ts#L119-L166)

### Text Content Types

| Kind | Description | Key Fields |
|------|-------------|------------|
| `"markdownContent"` | Markdown text | `content` (IMarkdownString with `value` property) |
| `"markdownVuln"` | Markdown with vulnerability info | `content`, `vulnerabilities` |
| `"thinking"` | Model's reasoning/thinking process | `value` (string or array) |
| `"warning"` | Warning message | `content` |
| `"progressMessage"` | Progress status message | `content` |

#### Markdown Content Structure

```typescript
{
  "kind": "markdownContent",
  "content": {
    "value": string,
    "isTrusted": boolean | undefined,
    "supportThemeIcons": boolean | undefined,
    "supportHtml": boolean | undefined,
    "baseUri": UriComponents | undefined
  },
  "inlineReferences": Record<string, IChatContentInlineReference> | undefined,
  "fromSubagent": boolean | undefined
}
```

### Interactive Content Types

| Kind | Description | Key Fields |
|------|-------------|------------|
| `"command"` | Clickable command button | `command` (with `id`, `title`, `arguments`) |
| `"confirmation"` | User confirmation request | `title`, `message`, `isUsed`, `buttons` |
| `"elicitation2"` / `"elicitationSerialized"` | User input request | `title`, `message`, `state` |

#### Confirmation Structure

```typescript
{
  "kind":  "confirmation",
  "title": string,
  "message":  string | IMarkdownString,
  "buttons": string[],
  "isUsed": boolean
}
```

### Code & File Types

| Kind | Description | Key Fields |
|------|-------------|------------|
| `"codeblockUri"` | URI for a code block | `uri`, `isEdit`, `undoStopId` |
| `"textEditGroup"` | Text file edits | `uri`, `edits`, `done`, `state` |
| `"notebookEditGroup"` | Notebook edits | `uri`, `edits`, `done` |
| `"treeData"` | File tree structure | `treeData` |
| `"multiDiffData"` | Multi-file diff view | `multiDiffData` with resources |

#### Text Edit Group Structure

```typescript
{
  "kind": "textEditGroup",
  "uri": UriComponents,
  "edits": TextEdit[][],
  "state": {
    "sha1": string,
    "applied": number
  } | undefined,
  "done":  boolean | undefined,
  "isExternalEdit": boolean | undefined
}
```

### Reference Types

| Kind | Description | Key Fields |
|------|-------------|------------|
| `"inlineReference"` | Inline citation/link | `inlineReference` (URI or Location), `name` |
| `"reference"` | Content reference | `reference`, `iconPath`, `options` |

### Tool Invocation Types

| Kind | Description | Key Fields |
|------|-------------|------------|
| `"toolInvocationSerialized"` | Completed tool call | `toolId`, `toolCallId`, `invocationMessage`, `pastTenseMessage`, `result`, `toolSpecificData` |
| `"progressTask"` / `"progressTaskSerialized"` | Async task progress | `content`, `progress` |

#### Tool Invocation Structure

```typescript
{
  "kind": "toolInvocationSerialized",
  "toolId": string,
  "toolCallId": string,
  "invocationMessage": string | IMarkdownString,
  "pastTenseMessage": string | IMarkdownString | undefined,
  "result": any | undefined,
  "resultDetails": any | undefined,
  "toolSpecificData": any | undefined,
  "isConfirmed": boolean | undefined
}
```

### Special Types

| Kind | Description | Key Fields |
|------|-------------|------------|
| `"extensions"` | Extension recommendations | `extensions` array |
| `"pullRequest"` | Pull request info | `owner`, `repo`, `pullRequestNumber` |
| `"undoStop"` | Undo checkpoint marker | `id` |
| `"mcpServersStarting"` | MCP servers starting | Server info |

---

## File Tree Data:  `IChatResponseProgressFileTreeData`

Used in `treeData` response parts. 

**Source**: [`chatService.ts`](https://github.com/microsoft/vscode/blob/1debf21160174ecaf114e8e043146da08ba25d4a/src/vs/workbench/contrib/chat/common/chatService.ts)

```typescript
{
  "label": string,
  "uri": string,
  "children": IChatResponseProgressFileTreeData[] | undefined
}
```

| Field | Description |
|-------|-------------|
| `label` | Display name of the file/folder |
| `uri` | URI to the resource |
| `children` | Nested items (for directories) |

---

## Agent Data: `ISerializableChatAgentData`

Information about the chat agent that handled the request.

**Source**: [`chatAgents.ts` lines 53-81](https://github.com/microsoft/vscode/blob/1debf21160174ecaf114e8e043146da08ba25d4a/src/vs/workbench/contrib/chat/common/chatAgents.ts#L53-L81)

```typescript
{
  "id": string,
  "name": string,
  "fullName": string | undefined,
  "description": string | undefined,
  "isDefault": boolean | undefined,
  "isDynamic": boolean | undefined,
  "isCore": boolean | undefined,
  "extensionId": { "value": string },
  "extensionVersion": string | undefined,
  "extensionPublisherId": string,
  "publisherDisplayName": string | undefined,
  "extensionDisplayName": string,
  "metadata": IChatAgentMetadata,
  "slashCommands": IChatAgentCommand[],
  "locations": ChatAgentLocation[],
  "modes": ChatModeKind[]
}
```

### Agent Metadata

```typescript
{
  "helpTextPrefix": string | IMarkdownString | undefined,
  "helpTextPostfix": string | IMarkdownString | undefined,
  "icon": URI | undefined,
  "iconDark": URI | undefined,
  "themeIcon": ThemeIcon | undefined,
  "sampleRequest": string | undefined,
  "supportIssueReporting": boolean | undefined,
  "followupPlaceholder": string | undefined,
  "isSticky": boolean | undefined
}
```

---

## Agent Result: `IChatAgentResult`

Final result/status information for a response.

**Source**:  [`chatAgents.ts` lines 167-175](https://github.com/microsoft/vscode/blob/1debf21160174ecaf114e8e043146da08ba25d4a/src/vs/workbench/contrib/chat/common/chatAgents.ts#L167-L175)

```typescript
{
  "errorDetails": IChatResponseErrorDetails | undefined,
  "timings": IChatAgentResultTimings | undefined,
  "metadata": Record<string, unknown> | undefined,
  "details": string | undefined,
  "nextQuestion": IChatQuestion | undefined
}
```

### Error Details

```typescript
{
  "message": string,
  "code": string | undefined,
  "responseIsRedacted": boolean | undefined,
  "responseIsFiltered": boolean | undefined,
  "responseIsIncomplete": boolean | undefined
}
```

| Field | Description |
|-------|-------------|
| `message` | Human-readable error message |
| `code` | Error code (e.g., `"canceled"`, `"rateLimited"`, `"quotaExceeded"`) |
| `responseIsRedacted` | If content was filtered/redacted |
| `responseIsFiltered` | If response was blocked by content filters |
| `responseIsIncomplete` | If response was cut off |

### Timings

```typescript
{
  "firstProgress": number | undefined,
  "totalElapsed": number
}
```

| Field | Description |
|-------|-------------|
| `firstProgress` | Time to first content in milliseconds |
| `totalElapsed` | Total response time in milliseconds |

### Next Question

```typescript
{
  "prompt": string,
  "participant": string | undefined,
  "command": string | undefined
}
```

---

## Response State:  `ResponseModelStateT`

The `modelState` field indicates the response completion status.

**Source**:  [`chatService.ts` lines 920-927](https://github.com/microsoft/vscode/blob/1debf21160174ecaf114e8e043146da08ba25d4a/src/vs/workbench/contrib/chat/common/chatService.ts#L920-L927)

```typescript
{
  "value": ResponseModelState,
  "completedAt": number | undefined
}
```

### ResponseModelState Values

| Value | Name | Description |
|-------|------|-------------|
| `0` | `Pending` | Response still generating |
| `1` | `Complete` | Successfully completed |
| `2` | `Cancelled` | User cancelled the request |
| `3` | `Failed` | An error occurred |
| `4` | `NeedsInput` | Waiting for user input/confirmation |

---

## User Feedback

**Source**: [`chatService.ts` lines 771-787](https://github.com/microsoft/vscode/blob/1debf21160174ecaf114e8e043146da08ba25d4a/src/vs/workbench/contrib/chat/common/chatService.ts#L771-L787)

### Vote Direction (`ChatAgentVoteDirection`)

| Value | Meaning |
|-------|---------|
| `0` | Down (unhelpful) |
| `1` | Up (helpful) |

### Vote Down Reasons (`ChatAgentVoteDownReason`)

When the user votes down, they may provide a reason: 

| Value | Meaning |
|-------|---------|
| `"incorrectCode"` | Suggested incorrect code |
| `"didNotFollowInstructions"` | Didn't follow instructions |
| `"incompleteCode"` | Incomplete code |
| `"missingContext"` | Missing context |
| `"poorlyWrittenOrFormatted"` | Poorly written or formatted |
| `"refusedAValidRequest"` | Refused a valid request |
| `"offensiveOrUnsafe"` | Offensive or unsafe |
| `"other"` | Other reason |
| `"willReportIssue"` | User will report an issue |

---

## Follow-ups:  `IChatFollowup`

Suggested follow-up questions. 

**Source**: [`chatService.ts` lines 753-769](https://github.com/microsoft/vscode/blob/1debf21160174ecaf114e8e043146da08ba25d4a/src/vs/workbench/contrib/chat/common/chatService.ts#L753-L769)

```typescript
{
  "kind": "reply",
  "message": string,
  "agentId": string,
  "subCommand": string | undefined,
  "title": string | undefined,
  "tooltip": string | undefined
}
```

| Field | Description |
|-------|-------------|
| `kind` | Always `"reply"` |
| `message` | The suggested question text |
| `agentId` | Target agent for the follow-up |
| `subCommand` | Slash command if applicable |
| `title` | Display title |
| `tooltip` | Hover tooltip text |

---

## Content References:  `IChatContentReference`

References cited in the response. 

**Source**: [`chatService.ts` lines 119-133](https://github.com/microsoft/vscode/blob/1debf21160174ecaf114e8e043146da08ba25d4a/src/vs/workbench/contrib/chat/common/chatService.ts#L119-L133)

```typescript
{
  "kind": "reference",
  "reference": URI | Location | IChatContentVariableReference | string,
  "iconPath": ThemeIcon | { "light": URI, "dark": URI } | undefined,
  "options": {
    "status": {
      "description": string,
      "kind": ChatResponseReferencePartStatusKind
    } | undefined,
    "diffMeta": {
      "added": number,
      "removed": number
    } | undefined,
    "originalUri": URI | undefined
  } | undefined
}
```

### Reference Status Kind

| Value | Name | Description |
|-------|------|-------------|
| `1` | `Complete` | Full content was used |
| `2` | `Partial` | Partial content was used |
| `3` | `Omitted` | Content was referenced but not used |

---

## Used Context:  `IChatUsedContext`

Documents/context that were used to generate the response. 

```typescript
{
  "kind": "usedContext",
  "documents": Array<{
    "uri":  URI,
    "version": number,
    "ranges": IRange[]
  }>
}
```

---

## Code Citations: `IChatCodeCitation`

Attribution for code that may match public repositories.

**Source**: [`chatService.ts` lines 135-141](https://github.com/microsoft/vscode/blob/1debf21160174ecaf114e8e043146da08ba25d4a/src/vs/workbench/contrib/chat/common/chatService.ts#L135-L141)

```typescript
{
  "kind": "codeCitation",
  "license": string,
  "snippet": string,
  "value": URI
}
```

| Field | Description |
|-------|-------------|
| `license` | License type (e.g., `"MIT"`, `"Apache-2.0"`) |
| `snippet` | The code snippet that was matched |
| `value` | Link to the source repository/file |

---

## Tool-Specific Data

For tool invocations, the `toolSpecificData` field contains tool-specific information. 

### Terminal Tool

```typescript
{
  "kind": "terminal",
  "commandLine": {
    "original": string,
    "toolEdited": string | undefined,
    "userEdited": string | undefined
  }
}
```

| Field | Description |
|-------|-------------|
| `original` | Original command suggested by the tool |
| `toolEdited` | Tool's modification to the command |
| `userEdited` | User's modification to the command |

---

## Edited File Events: `IChatAgentEditedFileEvent`

Records what happened to files edited during the chat session. 

**Source**: [`chatModel.ts` lines 2308-2317](https://github.com/microsoft/vscode/blob/1debf21160174ecaf114e8e043146da08ba25d4a/src/vs/workbench/contrib/chat/common/chatModel.ts#L2308-L2317)

```typescript
{
  "uri": URI,
  "eventKind": ChatRequestEditedFileEventKind
}
```

### Event Kind Values

| Value | Name | Description |
|-------|------|-------------|
| `1` | `Keep` | User accepted the edit |
| `2` | `Undo` | User rejected/undid the edit |
| `3` | `UserModification` | User modified the edit |

---

## Complete Example Export

```json
{
  "initialLocation": "panel",
  "responderUsername": "Copilot",
  "responderAvatarIconUri": { "id": "copilot" },
  "requests": [
    {
      "requestId": "request_a1b2c3d4-5678-90ab-cdef-1234567890ab",
      "message": {
        "text": "@workspace How do I create a REST API in Python?",
        "parts": [
          {
            "kind": "agent",
            "range": { "start": 0, "endExclusive": 10 },
            "editorRange": {
              "startLineNumber": 1,
              "startColumn": 1,
              "endLineNumber": 1,
              "endColumn": 11
            },
            "text": "@workspace",
            "agent": {
              "id":  "github. copilot. workspace",
              "name": "workspace"
            }
          },
          {
            "kind": "text",
            "range": { "start":  10, "endExclusive": 48 },
            "editorRange": {
              "startLineNumber": 1,
              "startColumn":  11,
              "endLineNumber": 1,
              "endColumn": 49
            },
            "text": " How do I create a REST API in Python?"
          }
        ]
      },
      "variableData": {
        "variables": [
          {
            "id": "vscode. file.abc123",
            "kind": "file",
            "name": "app.py",
            "value": { "scheme": "file", "path": "/project/app.py" }
          }
        ]
      },
      "response": [
        {
          "kind": "markdownContent",
          "content": {
            "value": "To create a REST API in Python, you can use **Flask** or **FastAPI**.  Here's a simple example using Flask:\n\n```python\nfrom flask import Flask, jsonify\n\napp = Flask(__name__)\n\n@app.route('/api/hello', methods=['GET'])\ndef hello():\n    return jsonify({'message': 'Hello, World!'})\n\nif __name__ == '__main__':\n    app.run(debug=True)\n```",
            "isTrusted": true
          }
        },
        {
          "kind": "inlineReference",
          "inlineReference": {
            "scheme": "file",
            "path": "/project/app.py"
          },
          "name": "app.py"
        }
      ],
      "responseId": "response_x9y8z7w6-5432-10ba-fedc-0987654321ba",
      "timestamp": 1703520000000,
      "agent": {
        "id": "github. copilot.workspace",
        "name": "workspace",
        "fullName": "Workspace",
        "isDefault": false,
        "extensionId": { "value": "github.copilot-chat" },
        "extensionDisplayName": "GitHub Copilot Chat"
      },
      "modelState": {
        "value": 1,
        "completedAt": 1703520005000
      },
      "result": {
        "timings": {
          "firstProgress": 150,
          "totalElapsed": 5000
        }
      },
      "vote": 1,
      "followups": [
        {
          "kind":  "reply",
          "message": "How do I add authentication to this API?",
          "agentId":  "github.copilot.workspace",
          "title": "Add authentication"
        },
        {
          "kind": "reply",
          "message": "How do I deploy this to production?",
          "agentId":  "github.copilot.workspace",
          "title": "Deploy to production"
        }
      ]
    },
    {
      "requestId": "request_b2c3d4e5-6789-01bc-def0-2345678901bc",
      "message": {
        "text": "Add a POST endpoint to create users",
        "parts": [
          {
            "kind":  "text",
            "range": { "start":  0, "endExclusive": 35 },
            "editorRange": {
              "startLineNumber": 1,
              "startColumn": 1,
              "endLineNumber": 1,
              "endColumn":  36
            },
            "text":  "Add a POST endpoint to create users"
          }
        ]
      },
      "variableData":  { "variables": [] },
      "response":  [
        {
          "kind": "markdownContent",
          "content": {
            "value":  "I'll add a POST endpoint to create users. Here's the updated code:",
            "isTrusted": true
          }
        },
        {
          "kind": "textEditGroup",
          "uri": { "scheme": "file", "path": "/project/app.py" },
          "edits": [
            [
              {
                "range": {
                  "startLineNumber": 8,
                  "startColumn": 1,
                  "endLineNumber": 8,
                  "endColumn": 1
                },
                "text": "\nusers = []\n\n@app.route('/api/users', methods=['POST'])\ndef create_user():\n    data = request.get_json()\n    users.append(data)\n    return jsonify(data), 201\n"
              }
            ]
          ],
          "done": true
        }
      ],
      "responseId": "response_y0z1a2b3-6543-21cb-afed-1098765432cb",
      "timestamp": 1703520060000,
      "modelState": {
        "value": 1,
        "completedAt": 1703520063000
      },
      "result": {
        "timings": {
          "firstProgress": 100,
          "totalElapsed": 3000
        }
      },
      "editedFileEvents": [
        {
          "uri": { "scheme":  "file", "path": "/project/app.py" },
          "eventKind": 1
        }
      ]
    }
  ]
}
```

---

## Notes for Analysis

### 1. Chronological Order
Requests appear in the order they were made during the session.  The first item in the `requests` array is the first message sent. 

### 2. Message Reconstruction
The full user message can be reconstructed from: 
- `message.text` (if available as a parsed object)
- Or by concatenating all `message.parts[]. text` values
- Or directly from `message` if it's a plain string (older format)

### 3. Response Reconstruction
The full response text can be reconstructed by:
- Concatenating `markdownContent` parts' `content. value` fields
- Including `inlineReference` parts as links/citations
- Ignoring internal types like `progressMessage`, `codeblockUri`, `undoStop`

### 4.  Timestamps
All timestamps are Unix milliseconds: 
- `timestamp` - When the request was made
- `modelState.completedAt` - When the response completed
- Duration can be calculated as `completedAt - timestamp`

### 5. Error Detection
Check for errors by:
- `result.errorDetails` being present
- `modelState.value === 2` for cancelled
- `modelState.value === 3` for failed

### 6. Tool Invocations
Look for `toolInvocationSerialized` parts to see: 
- What tools were called (`toolId`)
- Their invocation message (`invocationMessage`)
- Their result (`result`, `resultDetails`)
- Tool-specific data (`toolSpecificData`)

### 7. Edited Files
The `editedFileEvents` field and `textEditGroup`/`notebookEditGroup` response parts indicate files that were modified during the session: 
- `textEditGroup` contains the actual edits made
- `editedFileEvents` indicates whether the user accepted, rejected, or modified the changes

### 8. User Satisfaction
The `vote` field (when present) indicates whether the user found the response helpful: 
- `1` = Helpful (thumbs up)
- `0` = Unhelpful (thumbs down)
- `voteDownReason` provides additional context for negative feedback

### 9. Context Usage
The `usedContext` field shows which documents/files were used to generate the response, including specific line ranges.  The `contentReferences` field shows what was cited in the response. 

### 10. Agent Identification
The `agent` field identifies which chat agent handled the request: 
- `github.copilot.default` - Default Copilot agent
- `github.copilot.workspace` - Workspace-aware agent
- Other custom agents may be present

### 11. Follow-up Suggestions
The `followups` array contains suggested next questions that were offered to the user after each response. 