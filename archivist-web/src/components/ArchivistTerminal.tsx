import { useState, useEffect, useRef, type KeyboardEvent } from 'react';
import axios, { isAxiosError } from 'axios';
import { Typewriter } from 'react-simple-typewriter';
import { BinaryBackground } from '../BinaryBackground.tsx';
import { OmnissiahLogo } from './OmnissiahLogo.tsx';
import styles from './ArchivistTerminal.module.css';

interface ChatResponse {
    sessionId: string;
    question: string;
    answer: string;
}

interface MachineSpiritError {
    error: string;
    message: string;
}

interface Message {
    id: number;
    role: 'user' | 'archivist' | 'error';
    content: string;
}

const api = axios.create({
    baseURL: 'http://localhost:5185/api',
});

export const ArchivistTerminal = () => {
    const [isBooting, setIsBooting] = useState(true);
    const [input, setInput] = useState<string>('');
    const [messages, setMessages] = useState<Message[]>([]);
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const messagesEndRef = useRef<HTMLDivElement>(null);

    const CULT_QUOTE = "THERE IS NO CERTAINTY IN FLESH BUT ARTIFICE. PRAISE THE OMNISSIAH.";

    useEffect(() => {
        const timer = setTimeout(() => {
            setIsBooting(false);
        }, 4000); // Увеличил до 4с, чтобы успеть рассмотреть логотип
        return () => clearTimeout(timer);
    }, []);

    useEffect(() => {
        messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages]);

    const handleAsk = async () => {
        if (!input.trim() || input.length < 3) return;

        const userQuery = input.trim();
        const newUserMessage: Message = { id: Date.now(), role: 'user', content: userQuery };

        setMessages(prev => [...prev, newUserMessage]);
        setInput('');
        setIsLoading(true);

        try {
            const response = await api.get<ChatResponse>('/Lore/ask', {
                params: { question: userQuery, sessionId: 'terminal-session-1' }
            });

            const archivistMessage: Message = {
                id: Date.now() + 1,
                role: 'archivist',
                content: response.data.answer
            };
            setMessages(prev => [...prev, archivistMessage]);

        } catch (err: unknown) {
            let errorTitle = "CRITICAL FAILURE";
            let errorMessage = "Machine Spirit Unresponsive.";

            if (isAxiosError(err)) {
                const serverData = err.response?.data as MachineSpiritError | undefined;
                if (serverData) {
                    errorTitle = serverData.error;
                    errorMessage = serverData.message;
                } else {
                    errorMessage = err.message;
                }
            } else if (err instanceof Error) {
                errorMessage = err.message;
            }

            const errorMsgObject: Message = {
                id: Date.now() + 1,
                role: 'error',
                content: `${errorTitle}: ${errorMessage}`
            };
            setMessages(prev => [...prev, errorMsgObject]);
        } finally {
            setIsLoading(false);
        }
    };

    const handleKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
        if (e.key === 'Enter' && !isLoading) {
            handleAsk();
        }
    };

    if (isBooting) {
        return (
            <div className={styles.bootScreen}>
                <div className={styles.bootContent}>
                    <div className={styles.logoWrapper}>
                        <OmnissiahLogo />
                    </div>

                    <div className={styles.bootLogs}>
                        <p>{"&gt; BIOS V.44.02.1 INITIALIZING..."}</p>
                        <p>{"&gt; MACHINE SPIRIT DETECTED..."}</p>
                        <p>{"&gt; LOADING ARCHIVIST CORE V.2.0.1..."}</p>
                        <div className={styles.bootProgressBar} />
                        <p className={styles.blink}>ESTABLISHING WARP-LINK...</p>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className={`${styles.terminalContainer} ${styles.fadeIn}`}>
            <div className={`${styles.glitchOverlay} ${isLoading ? styles.glitchActive : ''}`} />

            <div className={styles.binaryBackground}>
                <BinaryBackground />
            </div>

            <header className={`${styles.frame} ${styles.headerFrame}`}>
                <h1 className={styles.title}>ARCHIVIST TERMINAL V.2.0.1</h1>
                <div className={styles.cultQuote}>{CULT_QUOTE}</div>
            </header>

            <div className={styles.mainContentWrapper}>
                <main className={`${styles.frame} ${styles.chatFrame}`}>
                    <div className={styles.messagesWindow}>
                        <div ref={messagesEndRef} />
                        {[...messages].reverse().map((msg) => (
                            <div key={msg.id} className={`${styles.message} ${styles[msg.role + 'Message']}`}>
                                {msg.role === 'user' && (
                                    <><span className={styles.userPrefix}>&gt; ACOLYTE:</span> {msg.content}</>
                                )}
                                {msg.role === 'archivist' && (
                                    <>
                                        <span className={styles.archivistPrefix}>&gt; ARCHIVIST:</span>
                                        {msg.id === [...messages].reverse().find(m => m.role === 'archivist')?.id ? (
                                            <Typewriter words={[msg.content]} typeSpeed={30} cursor cursorStyle='_' />
                                        ) : (
                                            ` ${msg.content}`
                                        )}
                                    </>
                                )}
                                {msg.role === 'error' && <>{msg.content}</>}
                            </div>
                        ))}
                    </div>

                    <div className={styles.inputArea}>
                        <span className={styles.promptPrefix}>[QUERY]&gt;</span>
                        <input
                            type="text"
                            className={styles.terminalInput}
                            value={input}
                            onChange={(e) => setInput(e.target.value)}
                            onKeyDown={handleKeyDown}
                            disabled={isLoading}
                            placeholder={isLoading ? "COGITATING..." : "INPUT DATA-STREAM..."}
                            autoFocus
                        />
                    </div>
                </main>

                <aside className={`${styles.frame} ${styles.dataPanelFrame}`}>
                    <h2 style={{ fontSize: '1em', textAlign: 'center', marginBottom: '15px' }}>STATUS</h2>

                    <div className={styles.dataGroup}>
                        <div className={styles.groupLabel}>// BIO-METRICS</div>
                        <div className={styles.dataItem}><span className={styles.dataLabel}>WARP INF:</span><span className={styles.dataValue}>0.001%</span></div>
                        <div className={styles.dataItem}><span className={styles.dataLabel}>SPIRIT:</span><span className={styles.dataValue}>ACTIVE</span></div>
                        <div className={styles.dataItem}><span className={styles.dataLabel}>STAMINA:</span><span className={styles.dataValue}>OPTIMAL</span></div>
                    </div>

                    <div className={styles.dataGroup}>
                        <div className={styles.groupLabel}>// LOGIC ARRAYS</div>
                        <div className={styles.dataItem}><span className={styles.dataLabel}>C# CORE:</span><span className={styles.dataValue}>STABLE</span></div>
                        <div className={styles.dataItem}><span className={styles.dataLabel}>VECTOR DB:</span><span className={styles.dataValue}>PG_VEC</span></div>
                        <div className={styles.dataItem}><span className={styles.dataLabel}>LORE HASH:</span><span className={styles.dataValue}>EF_CORE</span></div>
                        <div className={styles.dataItem}><span className={styles.dataLabel}>LATENCY:</span><span className={styles.dataValue}>12MS</span></div>
                    </div>

                    <div className={styles.dataGroup}>
                        <div className={styles.groupLabel}>// SESSION</div>
                        <div className={styles.dataItem}><span className={styles.dataLabel}>ID:</span><span className={styles.dataValue}>TERM_01</span></div>
                        <div className={styles.dataItem}><span className={styles.dataLabel}>MSG COUNT:</span><span className={styles.dataValue}>{messages.length}</span></div>
                        <div className={styles.dataItem}><span className={styles.dataLabel}>ACCESS:</span><span className={styles.dataValue}>GRANTED</span></div>
                    </div>

                    <div style={{marginTop: 'auto', color: '#a30000', fontSize: '0.6em', textAlign: 'center', opacity: 0.7}}>
                        &lt; NOOSPHERE ENCRYPTION ENABLED &gt; <br/>
                        &lt; PRAISE THE OMNISSIAH &gt;
                    </div>
                </aside>
            </div>
        </div>
    );
};