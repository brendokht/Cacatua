import React, { useEffect, useState } from "react";
import { Outlet, useNavigate } from "react-router-dom";
import { ScrollArea } from "../components/ui/ScrollArea";
import { Separator } from "../components/ui/Separator";
import { Button } from "../components/ui/Button";
import {
  ClipboardIcon,
  ExitIcon,
  FaceIcon,
  PersonIcon,
  PlusCircledIcon,
  PlusIcon,
  SunIcon,
} from "@radix-ui/react-icons";
import { Avatar, AvatarFallback, AvatarImage } from "../components/ui/Avatar";
import { useAuth } from "../contexts/AuthContext/AuthContext";
import {
  collection,
  doc,
  DocumentData,
  DocumentReference,
  getDoc,
  getDocs,
  query,
  where,
} from "firebase/firestore";
import { db } from "../firebase/auth";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "../components/ui/Dialog";
import { Input } from "../components/ui/Input";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
} from "../components/ui/Form";
import { createFlockSchema } from "../schemas/FlockCreation";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { RequestType } from "../enums/RequestType";
import { newChannelSchema } from "../schemas/NewChannelSchema";
interface ChatData {
  display: string;
  id: string;
}

interface ServerData {
  display: string;
  id: string;
}

interface FriendData {
  uid: string;
  displayName: string;
  photoUrl: string;
}

interface UserData {
  uid: string;
  displayName: string;
  photoUrl: string;
}

interface FlockMemberData {
  display: string;
  id: string;
  photoUrl: string;
  role: string;
}

interface RoleData {
  users?: Array<DocumentReference>;
  role?: string;
}

export default function MainLayout() {
  const [showMembers, setShowMembers] = useState<boolean>(false);
  const [channels, setChannels] = useState<Array<ChatData>>([]);
  const [flockMembers, setFlockMembers] = useState<Array<FlockMemberData>>([]);
  const [servers, setServers] = useState<Array<ServerData>>([]);
  const [friends, setFriends] = useState<Array<FriendData>>([]);
  const [useEffectFlag, setUseEffectFlag] = useState<boolean>(false);
  const [flockSentInvites, setFlockSentInvites] = useState<Array<string>>([]);
  const [users, setUsers] = useState<Array<UserData>>([]);

  const [updateCount, setUpdateCount] = useState(0);

  const handleUpdate = () => {
    setUpdateCount((prev) => ++prev);
  };

  const auth = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    grabUsersServers();
    grabFriendsList();
  }, []);

  useEffect(() => {
    onClickDMs();
    setUseEffectFlag(true);
  }, [useEffectFlag]);

  useEffect(() => {
    if (auth.selectedServer) {
      const fetchMembers = async () => {
        const docRef = doc(db, "flocks", auth.selectedServer);
        const docSnap = await getDoc(docRef);
        const flockData = docSnap.data();
        grabMembers(flockData);
      };

      fetchMembers();
    }
  }, [updateCount]);

  const form = useForm<z.infer<typeof createFlockSchema>>({
    resolver: zodResolver(createFlockSchema),
    defaultValues: {
      userId: auth.uid,
      flockName: "",
    },
  });

  const channelForm = useForm<z.infer<typeof newChannelSchema>>({
    resolver: zodResolver(newChannelSchema),
    defaultValues: {
      name: "",
    },
  });

  const createNewChannel = async (values: z.infer<typeof newChannelSchema>) => {
    const channel: ChatData = {
      display: values.name,
      id: values.name,
    };

    let previous = null;

    setChannels((prev) => {
      previous = prev;
      const newChannel = prev ? [...prev, channel] : [channel];
      return newChannel;
    });

    const response = await auth.fetchRequest(
      `Flock/create-new-channel-async?flockId=${auth.selectedServer}&name=${values.name}`,
      null,
      RequestType.POST
    );
    if (response.status !== 200) {
      console.error("Channel failed to create, reverting state");
      setChannels(previous);
    }
  };

  const onClickChat = async (e: React.MouseEvent<HTMLDivElement>) => {
    const flockId = e.currentTarget.getAttribute("data-key");
    auth.setSelectedChat(flockId);

    navigate("/");

    if (!auth.selectedServer) {
      const docRef = doc(db, "dms", flockId);
      const data = (await getDoc(docRef)).data();
      grabMembers(data);
    }
  };

  const grabFriendsList = async () => {
    const response = await auth.fetchRequest(
      "Friend/get-all-friend",
      null,
      RequestType.GET
    );

    const friendsList = await response.json();

    setFriends(friendsList);
  };

  const grabUsersServers = async () => {
    const q = query(
      collection(db, "flocks"),
      where("users", "array-contains", doc(db, `users/${auth.uid}`))
    );

    const snap = await getDocs(q);

    const usersServers: Array<ServerData> = [];

    const serverPromises = snap.docs.map(async (s) => {
      const serverDoc = s.data();
      const usersRef: Array<DocumentReference> = serverDoc["users"];
      const innerPromises = usersRef.map(async (u) => {
        const dataRef = await getDoc(u);
        const data = dataRef.data();
        if (data && data.uid === auth.uid) {
          usersServers.push({
            display: serverDoc["name"],
            id: s.id,
          });
        }
      });

      await Promise.all(innerPromises);
    });

    await Promise.all(serverPromises);

    setServers(usersServers);
  };

  const onClickServer = async (e: React.MouseEvent<HTMLDivElement>) => {
    navigate("/");
    auth.setSelectedServer(e.currentTarget.getAttribute("data-key"));
    auth.setSelectedChat("general");

    const docRef = doc(db, "flocks", e.currentTarget.getAttribute("data-key"));
    const docSnap = await getDoc(docRef);

    const flockData = docSnap.data();

    // grab channels
    const channelsRef: Array<string> = flockData["channels"];
    const channels: Array<ChatData> = [];
    if (channelsRef) {
      channelsRef.forEach((channelRef) => {
        const data: ChatData = {
          display: channelRef,
          id: channelRef,
        };
        channels.push(data);
      });
      setChannels(channels);
    } else {
      setChannels(null);
    }

    const owner: DocumentReference = flockData["owner"];

    if (owner) {
      const ref = await getDoc(owner);
      auth.setIsOwner(ref.id === auth.uid);
    } else {
      auth.setIsOwner(false);
    }

    if (flockData["sentInvites"]) {
      const sentInvites: Array<string> = flockData["sentInvites"];
      setFlockSentInvites(sentInvites);
    } else {
      setFlockSentInvites([]);
    }

    grabMembers(flockData);
  };

  const grabMembers = async (docData: DocumentData) => {
    // grab members
    const membersRef: Array<DocumentReference> = docData["users"];
    const members: Array<FlockMemberData> = [];

    const roles: Array<RoleData> = docData["roles"] ?? [];

    if (membersRef && roles) {
      const memberPromises = membersRef.map(async (memberRef) => {
        const memberDoc = await getDoc(memberRef);

        const member = {
          displayName: memberDoc.get("displayName"),
          uid: memberDoc.get("uid"),
          photoUrl: memberDoc.get("photoUrl"),
        } as UserData;

        const roleData = roles.find((r) =>
          r.users?.find((e) => e.id === memberRef.id)
        );
        const memberRole = roleData ? roleData.role ?? "Memeber" : "Member";

        const data: FlockMemberData = {
          display: member.displayName,
          id: member.uid,
          photoUrl: member.photoUrl,
          role: memberRole,
        };

        members.push(data);
      });
      await Promise.all(memberPromises);
      setFlockMembers(members);
    } else {
      setFlockMembers(null);
    }
  };

  async function createNewFlock(values: z.infer<typeof createFlockSchema>) {
    try {
      await auth.fetchRequest(
        "Flock/create-flock-async",
        values,
        RequestType.POST
      );
    } catch (err) {
      console.error("ERROR: ", err);
    } finally {
      grabUsersServers();
    }
  }

  const onClickDMs = async () => {
    navigate("/");
    auth.setSelectedServer(null);
    auth.setSelectedChat(null);
    setChannels([]);
    setFlockMembers([]);

    // Grab users' DMs
    const q = query(
      collection(db, "dms"),
      where("users", "array-contains", doc(db, `users/${auth.uid}`))
    );

    const snap = await getDocs(q);
    const users: Array<ChatData> = [];

    const userPromises = snap.docs.map(async (s) => {
      const dmDoc = s.data();
      const usersRef: Array<DocumentReference> = dmDoc["users"];
      const innerPromises = usersRef.map(async (u) => {
        const dataRef = await getDoc(u);
        const data = dataRef.data();
        if (data && data.uid !== auth.uid) {
          users.push({
            display: data.displayName,
            id: s.id,
          });
        }
      });

      await Promise.all(innerPromises);
    });

    await Promise.all(userPromises);

    setChannels(users);
  };

  const onClickRules = () => {
    auth.setSelectedChat("");
    navigate("/rules");
  };

  const onClickRoles = () => {
    auth.setSelectedChat("");
    navigate("/roles");
  };

  interface User {
    uid: string;
    email: string;
    displayName: string;
    photoUrl: string;
    emailVerified: boolean;
    disabled: boolean;
    tokensValidAfterTimestamp: string;
  }

  const getUsers = async () => {
    try {
      const response = await auth.fetchRequest(
        "Users/get-users",
        null,
        RequestType.GET
      );

      const responseData: User[] = await response.json();

      if (users.length < 1) {
        const newUids: UserData[] = responseData.map((user) => ({
          displayName: user.displayName,
          uid: user.uid,
          photoUrl: user.photoUrl,
        }));

        setUsers((prevUsers) => [...prevUsers, ...newUids]);
      }
    } catch (error) {
      console.error("Error when getting all users from the API: ", error);
    }
  };

  useEffect(() => {
    getUsers();
  }, []);

  const toggleTheme = () => {
    if (auth.theme === "dark") {
      auth.setTheme("light");
      localStorage.setItem("theme", "light");
      document.body.classList.add("light");
      document.body.classList.remove("dark");
    } else {
      auth.setTheme("dark");
      localStorage.setItem("theme", "dark");
      document.body.classList.add("dark");
      document.body.classList.remove("light");
    }
  };

  const hideMembersTab = () => {
    setShowMembers(!showMembers);
  };

  const mainClasses = `flex flex-row h-screen-fixed bg-background text-foreground ${auth.theme}`;

  return (
    <div className={mainClasses}>
      <div className="flex flex-row">
        <ScrollArea type="hover" className="border-r h-full px-3">
          <div
            onClick={() => onClickDMs()}
            className="hover:scale-105 hover:cursor-pointer transition-all duration-200 text-xs flex justify-center items-center border-2 border-foreground text-primary-foreground rounded-full bg-primary w-16 h-16 m-2"
            data-key={"DMs"}
          >
            <div className="font-bold text-xl">DMs</div>
          </div>
          <Separator />
          {servers.map((server) => (
            <div
              data-key={server.id}
              key={server.id}
              onClick={(e) => onClickServer(e)}
              className={`hover:scale-105 px-1 hover:cursor-pointer transition-all duration-200 text-center text-xs flex justify-center items-center border-2 border-foreground text-primary-foreground rounded-full w-16 h-16 m-2 ${
                auth.selectedServer === server.id
                  ? "bg-primary-selected"
                  : "bg-primary"
              }`}
            >
              <div>{server.display}</div>
            </div>
          ))}
          <Separator />
          <Dialog>
            <DialogTrigger asChild>
              <div
                data-key={"CreateFlock"}
                className="hover:scale-105 px-1 hover:cursor-pointer transition-all duration-200 text-center text-xs flex justify-center items-center border-2 border-foreground text-primary-foreground rounded-full bg-primary w-16 h-16 m-2"
              >
                <PlusIcon className="w-6 h-6" />
              </div>
            </DialogTrigger>
            <DialogContent className="sm:max-w-md">
              <DialogHeader>
                <DialogTitle>Create a Flock</DialogTitle>
                <DialogDescription>
                  Create a new Flock for your project or team!
                </DialogDescription>
              </DialogHeader>
              <div className="flex items-center space-x-2">
                <Form {...form}>
                  <form
                    onSubmit={form.handleSubmit(createNewFlock)}
                    className="space-y-4"
                  >
                    <FormField
                      control={form.control}
                      name="flockName"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Flock Name</FormLabel>
                          <FormControl>
                            <Input
                              placeholder="Super Cool Flock"
                              id="flockName"
                              type="text"
                              {...field}
                            />
                          </FormControl>
                        </FormItem>
                      )}
                    />
                    <DialogFooter className="sm:justify-start">
                      <DialogClose asChild>
                        <Button type="submit" variant="default">
                          Submit
                        </Button>
                      </DialogClose>
                      <DialogClose asChild>
                        <Button type="button" variant="secondary">
                          Close
                        </Button>
                      </DialogClose>
                    </DialogFooter>
                  </form>
                </Form>
              </div>
            </DialogContent>
          </Dialog>
        </ScrollArea>
        <ScrollArea
          type="hover"
          className="flex flex-col border-r h-full px-3 min-w-64"
        >
          <div className="flex flex-col min-h-full">
            <div className="flex flex-col gap-2">
              {channels
                ? channels.map((val, idx) => (
                    <div
                      data-key={val.id}
                      key={idx}
                      onClick={(e) => onClickChat(e)}
                      className={`hover:cursor-pointer text-xs flex transition-all justify-center items-center border-2 border-foreground text-primary-foreground rounded-lg w-full h-9 ${
                        auth.selectedChat === val.id
                          ? "bg-primary-selected"
                          : "bg-primary"
                      }`}
                    >
                      {val.display}
                    </div>
                  ))
                : null}
            </div>
            <div className="flex-1" />
            <div className="flex flex-row justify-evenly gap-2 items-center py-2">
              <Button
                variant="outline"
                size="lg"
                title="Profile"
                onClick={() => {
                  navigate(`/profile/${auth.uid}`);
                }}
                style={{ gap: 12 }}
              >
                {auth.username}
                <PersonIcon />
              </Button>
              <div className="flex-1" />
              <Button
                variant="outline"
                size="icon"
                title="Log out"
                onClick={() => {
                  auth.logOut();
                }}
              >
                <ExitIcon />
              </Button>
            </div>
          </div>
        </ScrollArea>
      </div>
      <div className="block px-2 flex-1 overflow-auto">
        <div className="sticky top-0 pb-2 flex flex-row justify-between bg-background">
          <div className="flex-1" />
          <div className="lg:space-x-4 space-x-2">
            {auth.selectedServer && auth.isOwner ? (
              <Dialog>
                <DialogTrigger asChild>
                  <Button
                    variant="outline"
                    size="icon"
                    title="Create new channel"
                  >
                    <PlusCircledIcon />
                  </Button>
                </DialogTrigger>
                <DialogContent className="sm:max-w-md">
                  <DialogHeader>
                    <DialogTitle>Create a new channel</DialogTitle>
                    <DialogDescription>
                      Create a new channel to chat in!
                    </DialogDescription>
                  </DialogHeader>
                  <div className="flex items-center space-x-2">
                    <Form {...channelForm}>
                      <form
                        onSubmit={channelForm.handleSubmit(createNewChannel)}
                        className="space-y-4"
                      >
                        <FormField
                          control={channelForm.control}
                          name="name"
                          render={({ field }) => (
                            <FormItem>
                              <FormLabel>Channel Name</FormLabel>
                              <FormControl>
                                <Input
                                  placeholder="Super Cool Channel"
                                  id="name"
                                  type="text"
                                  {...field}
                                />
                              </FormControl>
                            </FormItem>
                          )}
                        />
                        <DialogFooter className="sm:justify-start">
                          <DialogClose asChild>
                            <Button type="submit" variant="default">
                              Submit
                            </Button>
                          </DialogClose>
                          <DialogClose asChild>
                            <Button type="button" variant="secondary">
                              Close
                            </Button>
                          </DialogClose>
                        </DialogFooter>
                      </form>
                    </Form>
                  </div>
                </DialogContent>
              </Dialog>
            ) : null}
            <Button
              variant="outline"
              size="icon"
              onClick={toggleTheme}
              title="Toggle theme"
            >
              <SunIcon />
            </Button>
            <Button
              variant="outline"
              size="icon"
              onClick={hideMembersTab}
              title="Hide/Show Members Tab"
            >
              <PersonIcon />
            </Button>
            {auth.selectedServer ? (
              <>
                <Button
                  variant="outline"
                  size="icon"
                  onClick={onClickRules}
                  title="Rules"
                >
                  <ClipboardIcon />
                </Button>
                {auth.isOwner ? (
                  <Button
                    variant="outline"
                    size="icon"
                    onClick={onClickRoles}
                    title="Roles"
                  >
                    <FaceIcon />
                  </Button>
                ) : null}
              </>
            ) : null}
          </div>
        </div>
        <Outlet context={{ handleUpdate }} />
      </div>
      {!showMembers ? (
        <ScrollArea className="border-l w-64 px-4">
          <div className="text-xs font-bold my-2">
            Members - {flockMembers.length}
          </div>
          <div className="space-y-2">
            {Array.from(
              flockMembers
                .sort((a, b) => a.role.localeCompare(b.role))
                .reduce((acc, member) => {
                  if (!acc.has(member.role)) {
                    acc.set(member.role, []);
                  }
                  acc.get(member.role).push(member);
                  return acc;
                }, new Map())
            ).map(([role, members]) => (
              <div className="mt-4 space-y-2" key={role as string}>
                {auth.selectedServer ? (
                  <h2 className="text-xl font-semibold">{role as string}</h2>
                ) : null}
                {members.map((member: FlockMemberData) => (
                  <div
                    data-key={member.id}
                    key={member.id}
                    className="h-12 bg-primary text-primary-foreground border-2 border-foreground flex flex-row items-center space-x-2 px-2 rounded-md"
                  >
                    <Avatar>
                      <AvatarImage src={member.photoUrl} />
                      <AvatarFallback>
                        {member.display.charAt(0)}
                      </AvatarFallback>
                    </Avatar>
                    <div className="flex flex-col">
                      <div>{member.display}</div>
                    </div>
                  </div>
                ))}
              </div>
            ))}
            {auth.selectedServer ? (
              <Dialog>
                <DialogTrigger asChild>
                  <div className="hover:cursor-pointer h-12 bg-primary text-primary-foreground border-2 border-foreground flex flex-row items-center space-x-2 px-2 rounded-md">
                    <div className="flex flex-row w-full items-center">
                      <div>Invite People</div>
                      <div className="flex-1" />
                      <PlusIcon />
                    </div>
                  </div>
                </DialogTrigger>
                <DialogContent className="sm:max-w-[425px]">
                  <DialogHeader>
                    <DialogTitle>Invite People</DialogTitle>
                    <DialogDescription>
                      Invite your friends to the server!
                    </DialogDescription>
                  </DialogHeader>
                  <div className="space-y-2">
                    {friends.map((friend) => {
                      const isMember = flockMembers.some(
                        (member) => member.display === friend.displayName
                      );
                      const isInvited = flockSentInvites.includes(friend.uid);
                      return (
                        <div
                          key={friend.uid}
                          className="h-12 bg-primary text-primary-foreground border-2 border-foreground flex flex-row items-center space-x-2 px-2 rounded-md"
                        >
                          <Avatar>
                            <AvatarImage src={friend.photoUrl} />
                            <AvatarFallback>
                              {friend.displayName.charAt(0)}
                            </AvatarFallback>
                          </Avatar>
                          <div className="flex flex-col">
                            <div>{friend.displayName}</div>
                          </div>
                          <div className="flex-1" />
                          {!isMember ? (
                            isInvited ? (
                              <Button
                                variant="destructive"
                                size="sm"
                                onClick={async () => {
                                  const response = await auth.fetchRequest(
                                    `Flock/remove-invite-async?otherUid=${friend.uid}&flockId=${auth.selectedServer}`,
                                    null,
                                    RequestType.DELETE
                                  );
                                  if (response.status === 200) {
                                    setFlockSentInvites((prev) =>
                                      prev.filter((uid) => uid !== friend.uid)
                                    );
                                  }
                                }}
                              >
                                Cancel
                              </Button>
                            ) : (
                              <Button
                                variant="outline"
                                size="sm"
                                className={`${
                                  auth.theme === "dark"
                                    ? "text-white"
                                    : "text-black"
                                }`}
                                onClick={async () => {
                                  const response = await auth.fetchRequest(
                                    `Flock/send-invite-async?otherUid=${friend.uid}&flockId=${auth.selectedServer}`,
                                    null,
                                    RequestType.POST
                                  );
                                  if (response.status === 200) {
                                    setFlockSentInvites((prev) => [
                                      ...prev,
                                      friend.uid,
                                    ]);
                                  }
                                }}
                              >
                                Invite
                              </Button>
                            )
                          ) : (
                            <Button
                              variant="outline"
                              size="sm"
                              disabled={true}
                              className={`${
                                auth.theme === "dark"
                                  ? "text-white"
                                  : "text-black"
                              }`}
                            >
                              Invite
                            </Button>
                          )}
                        </div>
                      );
                    })}
                  </div>
                  <DialogFooter>
                    <DialogClose asChild>
                      <Button type="button" variant="default">
                        Done
                      </Button>
                    </DialogClose>
                  </DialogFooter>
                </DialogContent>
              </Dialog>
            ) : null}
          </div>
        </ScrollArea>
      ) : null}
    </div>
  );
}
