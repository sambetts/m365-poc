import { GraphLoader } from "./GraphLoader";
import { Chat, ChatMessage, DriveItem, Message, User } from "@microsoft/microsoft-graph-types";

const MAX_ITEMS : number = 5;

// App-specific implementation for GraphLoader
export class ExampleAppGraphLoader extends GraphLoader {

    loadUserProfile(): Promise<User> {
        return this.loadSingle<User>("/me");
    }

    loadEmails(): Promise<Message[]> {
        return this.loadList<Message[]>("/me/mailFolders/inbox/messages", MAX_ITEMS);
    }

    loadChats(): Promise<ChatMessage[]> {

        return this.loadList<Chat[]>("/me/chats", MAX_ITEMS).then((chatThreads: Chat[]) => {
            const chatThreadLoadpromises: Promise<ChatMessage[]>[] = [];
            chatThreads.forEach(t => {

                // Parallel load each thread chat messages
                if (t.id) {
                    chatThreadLoadpromises.push(this.loadChatMessages(t.id));        // Will return array of ChatMessages
                }
            });

            return Promise.all(chatThreadLoadpromises).then((chatThreadResponses: ChatMessage[][]) => {

                // Array of results (each result being an array of messages)
                const allReplies: ChatMessage[] = [];
                chatThreadResponses.forEach((threadReplyParent: ChatMessage[]) => {

                    // Combine all replies in all threads into one list
                    threadReplyParent.forEach((threadReplyChild: ChatMessage) => allReplies.push(threadReplyChild));

                });

                return Promise.resolve(allReplies);
            });
        });
    }

    loadChatMessages(chatId: string): Promise<ChatMessage[]> {
        return this.loadList<ChatMessage[]>(`/me/chats/${chatId}/messages`, MAX_ITEMS);
    }

    loadOneDriveFiles(): Promise<DriveItem[]> {
        return this.loadList<DriveItem[]>("/me/drive/root/children", MAX_ITEMS);
    }
}
