export type SupportConversationStatus = 'Open' | 'Assigned' | 'Closed';

export interface SupportConversation {
    id: number;
    subject: string;
    customerUserId: number;
    customerName: string;
    supportUserId: number | null;
    supportName: string | null;
    status: SupportConversationStatus;
    lastMessage: string | null;
    lastSenderRole: string | null;
    lastMessageAt: string | null;
    closedAt: string | null;
    createdAt: string;
}

export interface SupportMessage {
    id: number;
    conversationId: number;
    senderUserId: number;
    senderRole: string;
    senderName: string;
    message: string;
    isSystemMessage: boolean;
    createdAt: string;
}

export interface StartSupportConversationRequest {
    subject?: string;
    initialMessage?: string;
}

export interface SendSupportMessageRequest {
    message: string;
}

export interface AssignSupportConversationRequest {
    supportUserId: number;
}

export interface SupportQueueRequest {
    page?: number;
    pageSize?: number;
}

export interface SupportMessagesRequest {
    conversationId: number;
    page?: number;
    pageSize?: number;
}
