import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type { HubConnection } from '@microsoft/signalr';
import { MessageSquare, Send, UserCheck, XCircle } from 'lucide-react';
import { toast } from 'sonner';

import { useAppSelector } from '@/app/hooks';
import { Badge } from '@/components/common/badge';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/common/card';
import { Input } from '@/components/common/input';
import { Skeleton } from '@/components/common/skeleton';
import { Textarea } from '@/components/common/textarea';

import {
    useAssignSupportConversationMutation,
    useCloseSupportConversationMutation,
    useGetMySupportConversationsQuery,
    useGetSupportMessagesQuery,
    useGetSupportQueueQuery,
    useSendSupportMessageMutation,
    useStartSupportConversationMutation,
} from '@/features/support/supportApi';

import {
    assignConversationRealtime,
    closeConversationRealtime,
    createLiveSupportConnection,
    joinConversation,
    sendMessageRealtime,
} from '@/features/support/supportHub';

import type { SupportConversation, SupportConversationStatus } from '@/features/support/types';

const statusMap: Record<SupportConversationStatus, { label: string; className: string }> = {
    Open: { label: 'Açık', className: 'bg-emerald-100 text-emerald-800' },
    Assigned: { label: 'Atandı', className: 'bg-blue-100 text-blue-800' },
    Closed: { label: 'Kapalı', className: 'bg-zinc-200 text-zinc-700' },
};

function getStatusMeta(status: string) {
    if (status === 'Assigned') return statusMap.Assigned;
    if (status === 'Closed') return statusMap.Closed;
    return statusMap.Open;
}

function formatDate(value?: string | null) {
    if (!value) return '-';
    return new Date(value).toLocaleString('tr-TR', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
    });
}

function isIgnorableHubStartError(error: unknown): boolean {
    const message = error instanceof Error ? error.message : String(error ?? '');
    return (
        message.includes('stopped during negotiation') ||
        message.includes('AbortError') ||
        message.includes('Failed to complete negotiation')
    );
}

function isHubRateLimitError(error: unknown): boolean {
    const message = error instanceof Error ? error.message : String(error ?? '');
    return message.includes('RATE_LIMIT_EXCEEDED');
}

export default function Support() {
    const { user, token, isAuthenticated } = useAppSelector((state) => state.auth);

    const normalizedRole = (user?.role ?? '').trim().toLowerCase();
    const isCustomer = normalizedRole === 'customer';
    const isSupport = normalizedRole === 'support';
    const isAdmin = normalizedRole === 'admin';
    const isSupportOrAdmin = isSupport || isAdmin;

    const [selectedConversationId, setSelectedConversationId] = useState<number | null>(null);
    const [subject, setSubject] = useState('Canlı Destek');
    const [initialMessage, setInitialMessage] = useState('');
    const [messageText, setMessageText] = useState('');

    const hubRef = useRef<HubConnection | null>(null);
    const selectedConversationIdRef = useRef<number | null>(null);

    const {
        data: myConversations = [],
        isLoading: isMyConversationsLoading,
        refetch: refetchMyConversations,
    } = useGetMySupportConversationsQuery(undefined, {
        skip: !isAuthenticated || (!isCustomer && !isSupport),
    });

    const {
        data: queueData,
        isLoading: isQueueLoading,
        refetch: refetchQueue,
    } = useGetSupportQueueQuery(
        { page: 1, pageSize: 50 },
        {
            skip: !isAuthenticated || !isSupportOrAdmin,
        }
    );

    const conversations = useMemo<SupportConversation[]>(() => {
        if (isCustomer) return myConversations;

        if (isSupport) {
            const queueItems = queueData?.items ?? [];
            const merged = new Map<number, SupportConversation>();

            for (const item of queueItems) {
                merged.set(item.id, item);
            }

            for (const item of myConversations) {
                merged.set(item.id, item);
            }

            return Array.from(merged.values()).sort((a, b) => {
                const aDate = new Date(a.lastMessageAt || a.createdAt).getTime();
                const bDate = new Date(b.lastMessageAt || b.createdAt).getTime();
                return bDate - aDate;
            });
        }

        if (isAdmin) {
            return queueData?.items ?? [];
        }

        return [];
    }, [isAdmin, isCustomer, isSupport, myConversations, queueData]);

    const effectiveSelectedConversationId = useMemo<number | null>(() => {
        if (!conversations.length) return null;

        if (selectedConversationId && conversations.some((x) => x.id === selectedConversationId)) {
            return selectedConversationId;
        }

        return conversations[0].id;
    }, [conversations, selectedConversationId]);

    const selectedConversation = useMemo(
        () => conversations.find((x) => x.id === effectiveSelectedConversationId) ?? null,
        [conversations, effectiveSelectedConversationId]
    );

    const {
        data: messagesPage,
        isLoading: isMessagesLoading,
        refetch: refetchMessages,
    } = useGetSupportMessagesQuery(
        {
            conversationId: effectiveSelectedConversationId ?? 0,
            page: 1,
            pageSize: 200,
        },
        {
            skip: !effectiveSelectedConversationId,
        }
    );

    const messages = messagesPage?.items ?? [];

    const [startConversation, { isLoading: isStartingConversation }] = useStartSupportConversationMutation();
    const [sendSupportMessage, { isLoading: isSendingMessage }] = useSendSupportMessageMutation();
    const [assignSupportConversation, { isLoading: isAssigning }] = useAssignSupportConversationMutation();
    const [closeSupportConversation, { isLoading: isClosing }] = useCloseSupportConversationMutation();

    const refreshConversations = useCallback(async () => {
        if (isCustomer || isSupport) {
            await refetchMyConversations();
        }

        if (isSupportOrAdmin) {
            await refetchQueue();
        }
    }, [isCustomer, isSupport, isSupportOrAdmin, refetchMyConversations, refetchQueue]);

    useEffect(() => {
        selectedConversationIdRef.current = effectiveSelectedConversationId;
    }, [effectiveSelectedConversationId]);

    useEffect(() => {
        if (!isAuthenticated || !token) return;

        const connection = createLiveSupportConnection(() => token);
        hubRef.current = connection;
        let disposed = false;

        const handleRealtimeUpdate = () => {
            void refreshConversations();
            if (selectedConversationIdRef.current) {
                void refetchMessages();
            }
        };

        connection.on('ReceiveMessage', handleRealtimeUpdate);
        connection.on('ConversationAssigned', handleRealtimeUpdate);
        connection.on('ConversationClosed', handleRealtimeUpdate);

        connection.start().catch((error) => {
            if (disposed || isIgnorableHubStartError(error)) {
                return;
            }

            console.error('Live support hub start failed:', error);
        });

        return () => {
            disposed = true;
            connection.off('ReceiveMessage', handleRealtimeUpdate);
            connection.off('ConversationAssigned', handleRealtimeUpdate);
            connection.off('ConversationClosed', handleRealtimeUpdate);
            void connection.stop().catch(() => {
                // stop çağrısı race condition ile start öncesi gelebilir (dev strict mode)
            });
            if (hubRef.current === connection) hubRef.current = null;
        };
    }, [isAuthenticated, token, refreshConversations, refetchMessages]);

    useEffect(() => {
        if (!effectiveSelectedConversationId) return;
        if (!hubRef.current) return;

        joinConversation(hubRef.current, effectiveSelectedConversationId).catch((error) => {
            console.error('JoinConversation failed:', error);
        });
    }, [effectiveSelectedConversationId]);

    const handleStartConversation = async () => {
        if (!isCustomer) return;

        try {
            const created = await startConversation({
                subject: subject.trim() || 'Canlı Destek',
                initialMessage: initialMessage.trim() || undefined,
            }).unwrap();

            setSelectedConversationId(created.id);
            setInitialMessage('');
            await refreshConversations();
            toast.success('Destek görüşmesi hazır');
        } catch {
            toast.error('Görüşme başlatılamadı');
        }
    };

    const handleSendMessage = async () => {
        if (!effectiveSelectedConversationId) return;

        const message = messageText.trim();
        if (!message) return;

        setMessageText('');

        try {
            if (hubRef.current) {
                await sendMessageRealtime(hubRef.current, effectiveSelectedConversationId, message);
            } else {
                await sendSupportMessage({
                    conversationId: effectiveSelectedConversationId,
                    body: { message },
                }).unwrap();
            }

            await refetchMessages();
            await refreshConversations();
        } catch (error) {
            if (isHubRateLimitError(error)) {
                toast.error('Çok hızlı mesaj gönderiyorsun. Lütfen biraz bekle.');
                setMessageText(message);
                return;
            }

            try {
                await sendSupportMessage({
                    conversationId: effectiveSelectedConversationId,
                    body: { message },
                }).unwrap();

                await refetchMessages();
                await refreshConversations();
            } catch {
                toast.error('Mesaj gönderilemedi');
                setMessageText(message);
            }
        }
    };

    const handleAssignToMe = async () => {
        if (!isSupportOrAdmin || !effectiveSelectedConversationId || !user?.id) return;

        try {
            if (hubRef.current) {
                await assignConversationRealtime(hubRef.current, effectiveSelectedConversationId, user.id);
            } else {
                await assignSupportConversation({
                    conversationId: effectiveSelectedConversationId,
                    body: { supportUserId: user.id },
                }).unwrap();
            }

            await refreshConversations();
            await refetchMessages();
            toast.success('Görüşme üzerinize alındı');
        } catch {
            toast.error('Atama yapılamadı');
        }
    };

    const handleCloseConversation = async () => {
        if (!effectiveSelectedConversationId) return;

        try {
            if (hubRef.current) {
                await closeConversationRealtime(hubRef.current, effectiveSelectedConversationId);
            } else {
                await closeSupportConversation({ conversationId: effectiveSelectedConversationId }).unwrap();
            }

            await refreshConversations();
            await refetchMessages();
            toast.success('Görüşme kapatıldı');
        } catch {
            toast.error('Görüşme kapatılamadı');
        }
    };

    const isConversationsLoading = isCustomer
        ? isMyConversationsLoading
        : isSupport
            ? isMyConversationsLoading || isQueueLoading
            : isQueueLoading;
    const canAssignToMe =
        isSupportOrAdmin &&
        !!selectedConversation &&
        selectedConversation.status !== 'Closed' &&
        !selectedConversation.supportUserId &&
        !!user?.id;

    const isConversationClosed = selectedConversation?.status === 'Closed';
    const myUserId = user?.id ?? 0;

    return (
        <div className="container mx-auto px-4 py-8 space-y-6">
            <div className="flex items-center justify-between">
                <h1 className="text-2xl font-bold">Canlı Destek</h1>
                <Badge variant="secondary">{user?.role ?? 'Guest'}</Badge>
            </div>

            {isCustomer && (
                <Card>
                    <CardHeader>
                        <CardTitle className="text-base">Yeni Destek Talebi</CardTitle>
                    </CardHeader>
                    <CardContent className="grid gap-3">
                        <Input
                            value={subject}
                            onChange={(e) => setSubject(e.target.value)}
                            placeholder="Konu"
                            maxLength={250}
                        />
                        <Textarea
                            value={initialMessage}
                            onChange={(e) => setInitialMessage(e.target.value)}
                            placeholder="İlk mesaj (opsiyonel)"
                            rows={3}
                        />
                        <div className="flex justify-end">
                            <Button onClick={handleStartConversation} disabled={isStartingConversation}>
                                {isStartingConversation ? 'Başlatılıyor...' : 'Görüşme Başlat'}
                            </Button>
                        </div>
                    </CardContent>
                </Card>
            )}

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
                <Card className="lg:col-span-1">
                    <CardHeader>
                        <CardTitle className="text-base">
                            {isCustomer ? 'Görüşmelerim' : isSupport ? 'Destek Görüşmeleri' : 'Destek Kuyruğu'}
                        </CardTitle>
                    </CardHeader>
                    <CardContent>
                        {isConversationsLoading ? (
                            <div className="space-y-2">
                                <Skeleton className="h-14 w-full" />
                                <Skeleton className="h-14 w-full" />
                                <Skeleton className="h-14 w-full" />
                            </div>
                        ) : conversations.length === 0 ? (
                            <p className="text-sm text-muted-foreground">Henüz görüşme bulunmuyor.</p>
                        ) : (
                            <div className="space-y-2 max-h-[520px] overflow-y-auto">
                                {conversations.map((conversation) => {
                                    const status = getStatusMeta(conversation.status);
                                    const isActive = conversation.id === selectedConversationId;

                                    return (
                                        <button
                                            key={conversation.id}
                                            type="button"
                                            onClick={() => setSelectedConversationId(conversation.id)}
                                            className={`w-full rounded-md border p-3 text-left transition ${isActive ? 'border-primary bg-primary/5' : 'hover:bg-muted/60'
                                                }`}
                                        >
                                            <div className="flex items-center justify-between gap-2">
                                                <p className="truncate font-medium">{conversation.subject}</p>
                                                <span className={`rounded px-2 py-0.5 text-xs ${status.className}`}>
                                                    {status.label}
                                                </span>
                                            </div>
                                            <p className="mt-1 text-xs text-muted-foreground truncate">
                                                {conversation.lastMessage || 'Henüz mesaj yok'}
                                            </p>
                                            <p className="mt-1 text-[11px] text-muted-foreground">
                                                {formatDate(conversation.lastMessageAt || conversation.createdAt)}
                                            </p>
                                        </button>
                                    );
                                })}
                            </div>
                        )}
                    </CardContent>
                </Card>

                <Card className="lg:col-span-2">
                    <CardHeader>
                        <div className="flex flex-wrap items-center justify-between gap-2">
                            <CardTitle className="text-base">
                                {selectedConversation ? selectedConversation.subject : 'Görüşme seçin'}
                            </CardTitle>
                            {selectedConversation && (
                                <div className="flex items-center gap-2">
                                    {canAssignToMe && (
                                        <Button size="sm" variant="outline" onClick={handleAssignToMe} disabled={isAssigning}>
                                            <UserCheck className="h-4 w-4 mr-1" />
                                            Üstlen
                                        </Button>
                                    )}
                                    {!isConversationClosed && (
                                        <Button
                                            size="sm"
                                            variant="destructive"
                                            onClick={handleCloseConversation}
                                            disabled={isClosing}
                                        >
                                            <XCircle className="h-4 w-4 mr-1" />
                                            Kapat
                                        </Button>
                                    )}
                                </div>
                            )}
                        </div>
                    </CardHeader>

                    <CardContent>
                        {!selectedConversation ? (
                            <div className="h-[460px] flex items-center justify-center text-muted-foreground">
                                <div className="text-center">
                                    <MessageSquare className="mx-auto mb-2 h-10 w-10 opacity-50" />
                                    <p>Mesajları görmek için soldan bir görüşme seçin.</p>
                                </div>
                            </div>
                        ) : (
                            <div className="space-y-3">
                                <div className="h-[380px] overflow-y-auto rounded-md border p-3 space-y-3 bg-muted/20">
                                    {isMessagesLoading ? (
                                        <div className="space-y-2">
                                            <Skeleton className="h-12 w-2/3" />
                                            <Skeleton className="h-12 w-1/2 ml-auto" />
                                            <Skeleton className="h-12 w-3/4" />
                                        </div>
                                    ) : messages.length === 0 ? (
                                        <p className="text-sm text-muted-foreground">Henüz mesaj yok.</p>
                                    ) : (
                                        messages.map((message) => {
                                            if (message.isSystemMessage) {
                                                return (
                                                    <div key={message.id} className="text-center text-xs text-muted-foreground py-1">
                                                        {message.message}
                                                    </div>
                                                );
                                            }

                                            const isMine = message.senderUserId === myUserId;

                                            return (
                                                <div key={message.id} className={`flex ${isMine ? 'justify-end' : 'justify-start'}`}>
                                                    <div
                                                        className={`max-w-[80%] rounded-lg px-3 py-2 text-sm ${isMine ? 'bg-primary text-primary-foreground' : 'bg-background border'
                                                            }`}
                                                    >
                                                        <p className="whitespace-pre-wrap break-words">{message.message}</p>
                                                        <p
                                                            className={`mt-1 text-[11px] ${isMine ? 'text-primary-foreground/80' : 'text-muted-foreground'
                                                                }`}
                                                        >
                                                            {message.senderName} • {formatDate(message.createdAt)}
                                                        </p>
                                                    </div>
                                                </div>
                                            );
                                        })
                                    )}
                                </div>

                                <div className="flex items-center gap-2">
                                    <Input
                                        value={messageText}
                                        onChange={(e) => setMessageText(e.target.value)}
                                        onKeyDown={(e) => {
                                            if (e.key === 'Enter') {
                                                e.preventDefault();
                                                void handleSendMessage();
                                            }
                                        }}
                                        placeholder={isConversationClosed ? 'Bu görüşme kapalı' : 'Mesaj yaz...'}
                                        disabled={isConversationClosed || isSendingMessage}
                                    />
                                    <Button
                                        onClick={() => void handleSendMessage()}
                                        disabled={isConversationClosed || isSendingMessage || !messageText.trim()}
                                    >
                                        <Send className="h-4 w-4 mr-1" />
                                        Gönder
                                    </Button>
                                </div>
                            </div>
                        )}
                    </CardContent>
                </Card>
            </div>
        </div>
    );
}
