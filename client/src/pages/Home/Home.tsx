import { useEffect, useState, useRef } from "react";
import {
  collection,
  getDoc,
  onSnapshot,
  orderBy,
  query,
  Unsubscribe,
} from "firebase/firestore";
import { Button } from "../../components/ui/Button";
import { db } from "../../firebase/auth";
import { useAuth } from "../../contexts/AuthContext/AuthContext";
import { Input } from "../../components/ui/Input";
import { PlusIcon } from "@radix-ui/react-icons";
import { RequestType } from "../../enums/RequestType";

interface MessageData {
  id: string;
  text: string;
  timestamp: Date;
  user: string;
}

interface MessageSendData {
  uid: string;
  serverUid?: string;
  chatUid: string;
  text: string;
}

export default function Home() {
  const [messages, setMessages] = useState<Array<MessageData> | null>(null);
  const messagesEndRef = useRef<HTMLDivElement | null>(null);
  const auth = useAuth();

  useEffect(() => {
    let unsub: Unsubscribe | null = null;

    const fetchData = async () => {
      try {
        let q = null;
        if (auth.selectedChat != null) {
          if (auth.selectedServer == null) {
            q = query(
              collection(db, `dms/${auth.selectedChat}/messages`),
              orderBy("createdAt")
            );
          } else {
            q = query(
              collection(
                db,
                `flocks/${auth.selectedServer}/${auth.selectedChat}`
              ),
              orderBy("createdAt")
            );
          }
        } else {
          setMessages([]);
        }
        console.log("q =", q);
        if (q != null) {
          unsub = onSnapshot(q, async (querySnapshot) => {
            const msgArr: Array<MessageData> = await Promise.all(
              querySnapshot.docs.map(async (snap) => {
                const data = snap.data();
                const userData = (await getDoc(data.createdBy)).data() as {
                  displayName: string;
                };
                if (userData && userData.displayName) {
                  const time = data.createdAt.toDate();
                  return {
                    id: snap.id,
                    text: data.text,
                    timestamp: time,
                    user: userData.displayName,
                  };
                }
                return null;
              })
            ).then(
              (results) =>
                results.filter((msg) => msg !== null) as MessageData[]
            );
            setMessages(msgArr);
          });
        }
      } catch (error) {
        console.error("Error fetching data: ", error);
      }
    };

    fetchData();

    return () => {
      if (unsub) {
        unsub();
      }
    };
  }, [auth.selectedServer, auth.selectedChat]);

  useEffect(() => {
    if (messagesEndRef.current) {
      messagesEndRef.current.scrollIntoView({ behavior: "auto" });
    }
  }, [messages]);

  const onEnterPressed = async (e: React.KeyboardEvent<HTMLInputElement>) => {
    const text = e.currentTarget.value;

    if (e.key !== "Enter" || text === "") {
      return;
    }
    e.currentTarget.value = "";

    const messageBody: MessageSendData = {
      uid: auth.uid,
      text: text,
      serverUid: auth.selectedServer,
      chatUid: auth.selectedChat,
    };

    const message: MessageData = {
      id: new Date().toString(),
      text: text,
      timestamp: new Date(),
      user: auth.username || "",
    };

    let previous = null;

    setMessages((prev) => {
      previous = prev;
      const newMessages = prev ? [...prev, message] : [message];
      return newMessages;
    });

    const response = await auth.fetchRequest(
      "Message/send-message-async",
      messageBody,
      RequestType.POST
    );
    if (response.status !== 200) {
      console.error("Message failed to send, reverting state");
      setMessages(previous);
    }
  };

  return (
    <div className="flex flex-col h-[92.75%]">
      <div className="flex flex-col gap-2 overflow-y-auto">
        {messages && messages.length > 0
          ? messages.map((message) => (
              <div
                className="border border-primary rounded-md p-2 flex flex-col gap-1"
                key={message.id}
              >
                <div>
                  <span className="font-bold">{message.user}</span>{" "}
                  <span className="text-xs font-light">
                    at {message.timestamp.toLocaleDateString()}{" "}
                    {message.timestamp.toLocaleTimeString()}
                  </span>
                </div>
                <div>{message.text}</div>
              </div>
            ))
          : null}
        <div ref={messagesEndRef} />
      </div>
      <div className="flex-1" />
      {auth.selectedChat ? (
        <div className="flex flex-row justify-evenly sticky gap-2 py-2 bottom-0 bg-background">
          <Input onKeyDown={(e) => onEnterPressed(e)} />
        </div>
      ) : null}
    </div>
  );
}
