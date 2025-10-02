import React, { useEffect, useState, useRef } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext/AuthContext";
import { Button } from "../components/ui/Button";
import * as Avatar from "@radix-ui/react-avatar";
import UserInfo from "./interfaces/UserInfo";
import { ArrowLeftIcon, Cross2Icon } from "@radix-ui/react-icons";
import * as Dialog from "@radix-ui/react-dialog";
import "../styles/globals.css";
import { UserProfile } from "../contexts/AuthContext/interfaces/UserProfile";
import { RequestType } from "../enums/RequestType";

const ProfilePage: React.FC = () => {
  const auth = useAuth();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const { uid } = useParams<{ uid: string }>();
  const [userInfo, setUserInfo] = useState<UserInfo>({
    photo: "",
    displayName: "",
    alreadyFriends: false,
    friendRequestAlreadySent: false,
    friends: [],
    friendRequests: [],
    receivedFriendrequest: false,
    flockInvites: [] as { id: string }[],
  });

  const fileInputRef = useRef<HTMLInputElement | null>(null);

  const getProfileData = async (uid: string, token: string) => {
    try {
      const response: UserInfo = await auth.getProfileInfo(uid, token);
      setUserInfo(response);
      setLoading(false);
    } catch (error) {
      console.error("Error when getting profile data: " + error);
    }
  };

  useEffect(() => {
    if (uid) {
      setLoading(true);
      getProfileData(uid, auth.jwt);
    }
  }, [uid]);

  const handlePhotoClick = () => {
    if (fileInputRef.current) {
      fileInputRef.current.click();
    }
  };

  const handleFileChange = async (
    event: React.ChangeEvent<HTMLInputElement>
  ) => {
    const file = event.target.files?.[0];
    if (file) {
      const reader = new FileReader();
      reader.onloadend = async () => {
        const base64Img = reader.result as string;
        try {
          const response = await auth.fetchRequest(
            "Users/update-pfp",
            { file: base64Img, fileName: file.name },
            RequestType.POST
          );
          setUserInfo((prev) => ({ ...prev, photo: base64Img }));
        } catch (error) {
          console.error("Error updating profile picture: ", error);
        }
      };
      reader.readAsDataURL(file);
    }
  };

  //Add friend
  const handleAddFriend = async () => {
    try {
      const response: boolean = await auth.sendFriendRequest(uid, auth.jwt);
      if (response) {
        setUserInfo((prev) => ({ ...prev, friendRequestAlreadySent: true }));
      }
    } catch (error) {
      console.error("ERROR when adding friend: " + error);
    }
  };

  //Accept friend request
  const handleFriendRequest = async (
    friendUid: string,
    photo: string,
    displayName: string
  ) => {
    try {
      const response = await auth.acceptFriendRequest(friendUid, auth.jwt);
      if (response) {
        //Filter friend requests
        const _friendRequests = userInfo.friendRequests.filter(
          (user) => user.userInfo.uid !== friendUid
        );
        const _friends = userInfo.friends;
        _friends.push({
          userInfo: {
            displayName: displayName,
            photoUrl: photo,
            uid: friendUid,
          },
        });
        setUserInfo((prev) => ({
          ...prev,
          friendRequests: _friendRequests,
          alreadyFriends: true,
          friends: _friends,
        }));
      }
    } catch (error) {
      console.error("ERROR when adding friend: " + error);
    }
  };

  //Delete friend ()
  const handleDeleteFriend = async () => {
    //Gets rid of the friend in the array
    const _friends = userInfo.friends.filter(
      (user) => user.userInfo.uid !== auth.uid
    );
    try {
      const response = await auth.deleteFriendRequest(auth.uid, uid, auth.jwt);
      setUserInfo((prev) => ({
        ...prev,
        friends: _friends,
        alreadyFriends: false,
      }));
    } catch (error) {
      console.error("ERROR when deleting friend: " + error);
    }
  };

  const handleAcceptFriendInProfile = async () => {
    //Accept the friend request
    try {
      //uid = uid of the profile you're viewing
      const response: UserProfile = await auth.acceptFriendRequestInProfile(
        auth.uid,
        uid,
        auth.jwt
      );
      const _friends = userInfo.friends;
      _friends.push({
        userInfo: {
          displayName: response.displayName,
          photoUrl: response.photoUrl,
          uid: response.uid,
        },
      });

      setUserInfo((prev) => ({
        ...prev,
        friends: _friends,
        alreadyFriends: true,
        receivedFriendrequest: false,
      }));
    } catch (error) {
      console.error("ERROR when adding friend: " + error);
    }
  };

  const handleAcceptInvite = async (flockId: string) => {
    try {
      // await auth.fetchRequest(invite.id, auth.jwt);
      const response = await auth.fetchRequest(
        `Flock/accept-invite-async?otherUid=${uid}&flockId=${flockId}`,
        null,
        RequestType.POST
      );
      if (response.status === 200) {
        setUserInfo((prev) => ({
          ...prev,
          flockInvites: prev.flockInvites.filter(
            (invite) => invite.uid !== flockId
          ),
        }));
      }
    } catch (error) {
      console.error("ERROR when accepting invite: " + error);
    }
  };

  const handleDeclineInvite = async (flockId: string) => {
    try {
      // await auth.fetchRequest(invite.id, auth.jwt);
      const response = await auth.fetchRequest(
        `Flock/remove-invite-async?otherUid=${uid}&flockId=${flockId}`,
        null,
        RequestType.DELETE
      );
      if (response.status === 200) {
        setUserInfo((prev) => ({
          ...prev,
          flockInvites: prev.flockInvites.filter(
            (invite) => invite.uid !== flockId
          ),
        }));
      }
    } catch (error) {
      console.error("ERROR when declining invite: " + error);
    }
  };

  return (
    <div className="h-full">
      {loading ? (
        <div className="flex justify-center items-center h-screen">
          <div className="w-16 h-16 border-4 border-blue-500 border-solid border-t-transparent rounded-full animate-spin"></div>
        </div>
      ) : (
        <div className="grid grid-cols-3 gap-x-4 p-4 h-full">
          <div
            className={`flex flex-col items-center border-r pr-4 relative ${
              uid !== auth.uid ? "col-span-2" : ""
            }`}
          >
            <button
              onClick={() => {
                setLoading(true);
                navigate(-1);
              }}
              className="absolute top-0 left-0 mt-2 ml-2 flex items-center space-x-1 text-gray-600 hover:text-gray-900 transition"
            >
              <ArrowLeftIcon className="w-5 h-5" />
              <span className="text-sm font-medium">Back</span>
            </button>
            <button
              onClick={() => navigate("/")}
              className="absolute top-0 left-15 mt-2 ml-2 flex items-center space-x-1 text-gray-600 hover:text-gray-900 transition"
            >
              <ArrowLeftIcon className="w-5 h-5" />
              <span className="text-sm font-medium">Home</span>
            </button>

            {userInfo.photo && userInfo.displayName && (
              <>
                <Avatar.Root
                  className={`AvatarRoot mt-10 ${
                    uid === auth.uid ? "cursor-pointer" : ""
                  }`}
                  onClick={uid === auth.uid ? handlePhotoClick : null}
                >
                  <Avatar.Image
                    className="AvatarImage rounded-full w-32 h-32"
                    src={userInfo.photo}
                    alt="Profile Picture"
                  />
                </Avatar.Root>

                <input
                  ref={fileInputRef}
                  type="file"
                  accept="image/*"
                  className="hidden"
                  onChange={handleFileChange}
                  disabled={uid !== auth.uid}
                />
                <h1 className="mt-4 text-xl font-semibold">
                  {userInfo.displayName}
                </h1>
                {uid !== auth.uid && userInfo.alreadyFriends && (
                  <Dialog.Root>
                    <Dialog.Trigger asChild>
                      <Button id="delete-friend">Delete Friend</Button>
                    </Dialog.Trigger>
                    <Dialog.Portal>
                      <Dialog.Overlay className="DialogOverlay" />
                      <Dialog.Content className="DialogContent">
                        <Dialog.Title className="DialogTitle">
                          Delete {userInfo.displayName}?
                        </Dialog.Title>
                        <Dialog.Close asChild>
                          <button className="IconButton" aria-label="Close">
                            <Cross2Icon />
                          </button>
                        </Dialog.Close>
                        <div
                          style={{
                            display: "flex",
                            marginTop: 25,
                            justifyContent: "center",
                            gap: "5%",
                          }}
                        >
                          <Dialog.Close asChild>
                            <Button onClick={() => handleDeleteFriend()}>
                              Yes
                            </Button>
                          </Dialog.Close>
                          <Dialog.Close asChild>
                            <Button>No</Button>
                          </Dialog.Close>
                        </div>
                        <Dialog.Close asChild>
                          <button className="IconButton" aria-label="Close">
                            <Cross2Icon />
                          </button>
                        </Dialog.Close>
                      </Dialog.Content>
                    </Dialog.Portal>
                  </Dialog.Root>
                )}
                {uid !== auth.uid &&
                  !userInfo.alreadyFriends &&
                  (userInfo.receivedFriendrequest ? (
                    <Button
                      id="add-friend"
                      size="lg"
                      className="mt-auto"
                      disabled={userInfo.friendRequestAlreadySent}
                      onClick={() => handleAcceptFriendInProfile()}
                    >
                      Accept Friend Request
                    </Button>
                  ) : (
                    <Button
                      id="add-friend"
                      size="lg"
                      className="mt-auto"
                      disabled={userInfo.friendRequestAlreadySent}
                      onClick={() => handleAddFriend()}
                    >
                      {userInfo.friendRequestAlreadySent
                        ? "Friend Request Sent"
                        : "Add Friend"}
                    </Button>
                  ))}
              </>
            )}
          </div>
          {auth.uid === uid ? (
            <div className="flex flex-col items-center border-r pr-4 relative">
              <h2 className="text-lg font-semibold mb-2">Flock Invites</h2>
              <ul className="mb-4 space-y-2">
                {userInfo.flockInvites?.map((invite, index) => (
                  <div
                    key={index}
                    className="flex flex-row items-center space-x-2"
                  >
                    <li>
                      <p>{invite.name}</p>
                    </li>
                    <div className="flex space-x-2">
                      <Button
                        size="sm"
                        onClick={() => handleAcceptInvite(invite.uid)}
                      >
                        Accept
                      </Button>
                      <Button
                        size="sm"
                        onClick={() => handleDeclineInvite(invite.uid)}
                      >
                        Decline
                      </Button>
                    </div>
                  </div>
                ))}
              </ul>
            </div>
          ) : null}
          <div className="flex flex-col">
            <h2 className="text-lg font-semibold mb-2">Friends</h2>
            <ul className="mb-4 space-y-2">
              {userInfo.friends?.map((friend, index) => (
                <li key={index} className="border-b pb-2">
                  <Avatar.Root className={`AvatarRoot mt-10`}>
                    <Avatar.Image
                      className="AvatarImage rounded-full w-16 h-16 cursor-pointer"
                      src={friend.userInfo.photoUrl}
                      alt="Profile Picture"
                      onClick={() =>
                        navigate(`/profile/${friend.userInfo.uid}`)
                      }
                      title="Profile Picture"
                    />
                  </Avatar.Root>
                  <p>{friend.userInfo.displayName}</p>
                </li>
              ))}
            </ul>

            {auth.uid === uid && (
              <>
                <h2 className="text-lg font-semibold mb-2">Friend Requests</h2>
                <ul className="mb-4 space-y-2">
                  {userInfo.friendRequests?.map((request, index) => (
                    <li key={index} className="border-b pb-2">
                      <Avatar.Root className={`AvatarRoot mt-10`}>
                        <Avatar.Image
                          className="AvatarImage rounded-full w-16 h-16 cursor-pointer"
                          src={request.userInfo.photoUrl}
                          alt="Profile Picture"
                          onClick={() =>
                            navigate(`/profile/${request.userInfo.uid}`)
                          }
                          title="Profile Picture"
                        />
                      </Avatar.Root>
                      <p>{request.userInfo.displayName}</p>
                      <Dialog.Root>
                        <Dialog.Trigger asChild>
                          <Button id="accept-friend">
                            Accept Friend Request
                          </Button>
                        </Dialog.Trigger>
                        <Dialog.Portal>
                          <Dialog.Overlay className="DialogOverlay" />
                          <Dialog.Content className="DialogContent">
                            <Dialog.Title className="DialogTitle">
                              {" "}
                              Add {userInfo.displayName}?
                            </Dialog.Title>
                            <Dialog.Close asChild>
                              <button className="IconButton" aria-label="Close">
                                <Cross2Icon />
                              </button>
                            </Dialog.Close>
                            <div
                              style={{
                                display: "flex",
                                marginTop: 25,
                                justifyContent: "center",
                                gap: "5%",
                              }}
                            >
                              <Dialog.Close asChild>
                                <Button
                                  onClick={() => {
                                    handleFriendRequest(
                                      request.userInfo.uid,
                                      request.userInfo.photoUrl,
                                      request.userInfo.displayName
                                    );
                                  }}
                                >
                                  Yes
                                </Button>
                              </Dialog.Close>
                              <Dialog.Close asChild>
                                <Button>No</Button>
                              </Dialog.Close>
                            </div>
                            <Dialog.Close asChild>
                              <button className="IconButton" aria-label="Close">
                                <Cross2Icon />
                              </button>
                            </Dialog.Close>
                          </Dialog.Content>
                        </Dialog.Portal>
                      </Dialog.Root>
                    </li>
                  ))}
                </ul>
              </>
            )}
          </div>
        </div>
      )}
    </div>
  );
};

export default ProfilePage;
