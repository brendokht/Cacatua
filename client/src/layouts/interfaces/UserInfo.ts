export default interface UserInfo {
  photo: string;
  displayName: string;
  alreadyFriends?: boolean;
  friendRequestAlreadySent?: boolean;
  receivedFriendrequest?: boolean;
  //TODO:
  //Create an interface that friends and friendRequestsReceived meet
  friends?: any[];
  friendRequests?: any[];
  flockInvites?: any[];
}
