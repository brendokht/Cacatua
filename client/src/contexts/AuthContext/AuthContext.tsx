import {
  createContext,
  useContext,
  useState,
  ReactNode,
  useEffect,
} from "react";
import { registerAction, SignupReturnData } from "./Register";
import { signupFormSchema } from "../../schemas/RegisterSchema";
import { z } from "zod";
import UserInfo from "../../layouts/interfaces/UserInfo";
// import { LoginReturnData } from "./interfaces/LoginReturnData";
// import { LoginData } from "./interfaces/LoginData";
// import { ReturnUserData } from "./interfaces/ReturnUserData";
import { UserProfile } from "./interfaces/UserProfile";
import { RequestType } from "../../enums/RequestType";
// Define the structure of the context statez
type ThemeTypes = "light" | "dark";
interface AuthContextType {
  uid: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  photoURL: string;
  jwt: string;
  refresh: string;
  selectedChat: string;
  setSelectedChat: React.Dispatch<React.SetStateAction<string>>;
  isOwner: boolean;
  setIsOwner: React.Dispatch<React.SetStateAction<boolean>>;
  selectedServer: string;
  setSelectedServer: React.Dispatch<React.SetStateAction<string>>;
  theme: ThemeTypes;
  setTheme: React.Dispatch<React.SetStateAction<string>>;
  loginAction: (data: LoginData) => Promise<LoginReturnData>;
  logOut: () => void;
  getProfileInfo: (uid: string, token: string) => Promise<UserInfo>;
  acceptFriendRequest: (uid: string, token: string) => Promise<ProfileData>;
  sendFriendRequest: (uid: string, token: string) => Promise<boolean>;
  deleteFriendRequest: (
    uid: string,
    friendUid: string,
    token: string
  ) => Promise<boolean>;
  acceptFriendRequestInProfile: (
    uid: string,
    friendUid: string,
    token: string
  ) => Promise<ProfileData>;
  verifyJwt: () => Promise<boolean>;
  registerAction: (
    values: z.infer<typeof signupFormSchema>
  ) => Promise<SignupReturnData>;
  fetchRequest: (
    endpoint: string,
    data: unknown,
    type: RequestType
  ) => Promise<Response>;
}

interface LoginData {
  email?: string;
  password?: string;
  rememberMe?: boolean;
}

interface LoginReturnData {
  success: boolean;
  message?: string;
}

interface ProfileData {
  uid: string;
  displayName: string;
  photoUrl: string;
}

interface UserData {
  // Add more fields as necessary
  uid: string;
  displayName: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  photoUrl: string;
  rememberUser: boolean;
}

interface ReturnUserData {
  user: UserData;
  jwt: string;
  refresh: string;
  message: string;
}

// Create the AuthContext with a default value of null
const AuthContext = createContext<AuthContextType | null>(null);

// Define the props for AuthProvider
interface AuthProviderProps {
  children: ReactNode; // ReactNode is used to represent JSX children
}

const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [uid, setUid] = useState<string>("");
  const [username, setUsername] = useState<string>("");
  const [email, setEmail] = useState<string>("");
  const [firstName, setFirstName] = useState<string>("");
  const [lastName, setLastName] = useState<string>("");
  const [photoURL, setPhotoURL] = useState<string>("");
  const [isOwner, setIsOwner] = useState<boolean>(false);
  // const [rememberUser, setRememberUser] = useState<boolean>(false);
  const [theme, setTheme] = useState<ThemeTypes>("light");
  const [jwt, setJWT] = useState<string>(
    localStorage.getItem("jwt") || sessionStorage.getItem("jwt") || ""
  );
  const [refresh, setRefresh] = useState<string>(
    localStorage.getItem("refresh") || sessionStorage.getItem("refresh") || ""
  );

  const [selectedServer, setSelectedServer] = useState<string>();
  const [selectedChat, setSelectedChat] = useState<string>();

  useEffect(() => {
    const theme: ThemeTypes = localStorage.getItem("theme") as ThemeTypes;
    setTheme(theme);
    document.body.classList.add(theme);
  }, []);

  const getProfileInfo = async (
    uid: string,
    token: string
  ): Promise<UserInfo> => {
    try {
      // response = await fetch(
      //   `https://localhost:7297/api/Users/get-profile-data?uid=${encodeURIComponent(
      //     uid
      //   )}`,
      //   {
      //     method: "GET",
      //     headers: {
      //       "Content-Type": "application/json",
      //       Authorization: `Bearer ${token}`,
      //     },
      //   }
      // );
      const response = await fetchRequest(
        `Users/get-profile-data?uid=${encodeURIComponent(uid)}`,
        null,
        RequestType.GET
      );

      if (response.ok) {
        const data = await response.json();
        return data;
      }
    } catch (error) {
      const returnData: UserInfo = {
        displayName: "N/A",
        photo: "N/A",
      };
      console.error(error);
      return returnData;
    }
  };

  const loginAction = async (data: LoginData): Promise<LoginReturnData> => {
    const postData = {
      email: data.email,
      password: data.password,
    };

    let response;
    let responseData: ReturnUserData;
    try {
      response = await fetch("https://localhost:7297/api/Auth/login", {
        method: "POST",
        headers: {
          "Content-Type": "application/json;charset=UTF-8",
        },
        body: JSON.stringify(postData),
      });

      responseData = await response.json();

      const returnData: LoginReturnData = {
        success: response.ok,
        message: responseData.message,
      };
      if (response.ok) {
        const userStr = JSON.stringify({
          displayName: responseData.user.displayName,
          email: responseData.user.email,
          firstName: responseData.user.firstName,
          lastName: responseData.user.lastName,
          photoUrl: responseData.user.photoUrl,
          phoneNumber: responseData.user.phoneNumber,
          uid: responseData.user.uid,
          rememberMe: data.rememberMe,
        });

        if (data.rememberMe) {
          localStorage.setItem("user", userStr);
          localStorage.setItem("jwt", responseData.jwt);
          localStorage.setItem("refresh", responseData.refresh);
        } else {
          sessionStorage.setItem("jwt", responseData.jwt);
          sessionStorage.setItem("refresh", responseData.refresh);
          sessionStorage.setItem("user", userStr);
        }

        setJWT(responseData.jwt);
        setRefresh(responseData.refresh);
        setUid(responseData.user.uid);
        setUsername(responseData.user.displayName);
        setEmail(responseData.user.email);
        setFirstName(responseData.user.firstName);
        setLastName(responseData.user.lastName);
        setPhotoURL(responseData.user.photoUrl);
        // setRememberUser(data.rememberMe);
      }
      return returnData;
    } catch (err) {
      const returnData: LoginReturnData = {
        success: response.ok,
        message: responseData.message ?? null,
      };
      return returnData;
    }
  };

  const logOut = async (): Promise<boolean> => {
    const response = await fetch("https://localhost:7297/api/Auth/logout", {
      method: "POST",
      headers: {
        "Content-Type": "application/json;charset=UTF-8",
        Authorization: `Bearer ${jwt}`,
      },
      body: JSON.stringify(uid),
    });

    if (!response.ok) {
      return false;
    }

    setUid("");
    setUsername("");
    setEmail("");
    setFirstName("");
    setLastName("");
    setPhotoURL("");
    setIsOwner(false);
    setJWT("");
    setRefresh("");
    setSelectedChat("");
    setSelectedServer("");

    localStorage.removeItem("user");
    localStorage.removeItem("jwt");
    localStorage.removeItem("refresh");
    sessionStorage.removeItem("user");
    sessionStorage.removeItem("jwt");
    sessionStorage.removeItem("refresh");
    return true;
  };

  const verifyJwt = async () => {
    let token = "";
    token = localStorage.getItem("jwt");
    if (!token) {
      token = sessionStorage.getItem("jwt");
      if (!token) {
        localStorage.removeItem("user");
        localStorage.removeItem("jwt");
        localStorage.removeItem("refresh");
        sessionStorage.removeItem("user");
        sessionStorage.removeItem("jwt");
        sessionStorage.removeItem("refresh");
        return false;
      }
    }
    try {
      const response = await fetch(
        `https://localhost:7297/api/Auth/check-jwt?jwtToken=${token}`
      );
      let user: UserData = JSON.parse(localStorage.getItem("user"));
      if (user === null) {
        user = JSON.parse(sessionStorage.getItem("user"));
      }
      if (response.ok && user !== null) {
        setUid(user.uid);
        setUsername(user.displayName);
        setEmail(user.email);
        setFirstName(user.firstName);
        setLastName(user.lastName);
        setPhotoURL(user.photoUrl);
        // setRememberUser(user.rememberUser);
      }
      return response.ok;
    } catch (error) {
      console.error("Error fetching data:", error);
      localStorage.removeItem("jwt");
      localStorage.removeItem("user");
      sessionStorage.removeItem("jwt");
      sessionStorage.removeItem("user");
      return false;
    }
  };

  const fetchRequest = async (
    endpoint: string,
    data: unknown,
    type: RequestType
  ): Promise<Response> => {
    try {
      // Attempt the API call with the current JWT token
      let response;
      if (data === null) {
        response = await fetch(`https://localhost:7297/api/${endpoint}`, {
          method: RequestType[type],
          headers: {
            "Content-Type": "application/json;charset=UTF-8",
            Authorization: `Bearer ${jwt}`,
          },
        });
      } else {
        response = await fetch(`https://localhost:7297/api/${endpoint}`, {
          method: RequestType[type],
          headers: {
            "Content-Type": "application/json;charset=UTF-8",
            Authorization: `Bearer ${jwt}`,
          },
          body: JSON.stringify(data),
        });
      }

      if (response.status === 401) {
        console.error("JWT EXPIRED!!!");
        const refreshResponse = await refreshToken();
        if (refreshResponse.ok) {
          const newJwt = await refreshResponse.json();
          setJWT(newJwt.jwt);
          setRefresh(newJwt.refreshToken);
          if (localStorage.getItem("jwt")) {
            localStorage.removeItem("jwt");
            localStorage.setItem("jwt", newJwt.jwt);
            localStorage.removeItem("refresh");
            localStorage.setItem("refresh", newJwt.refreshToken);
          } else if (sessionStorage.getItem("jwt")) {
            sessionStorage.removeItem("jwt");
            sessionStorage.setItem("jwt", newJwt.jwt);
            sessionStorage.removeItem("refresh");
            sessionStorage.setItem("refresh", newJwt.refreshToken);
          }

          const response = await fetch(
            `https://localhost:7297/api/${endpoint}`,
            {
              method: "POST",
              headers: {
                "Content-Type": "application/json;charset=UTF-8",
                Authorization: `Bearer ${newJwt.jwt}`,
              },
              body: JSON.stringify(data),
            }
          );
          return response;
        } else {
          console.error("Failed to refresh the token");
          setUid("");
          setUsername("");
          setEmail("");
          setFirstName("");
          setLastName("");
          setPhotoURL("");
          setIsOwner(false);
          setJWT("");
          setRefresh("");
          setSelectedChat("");
          setSelectedServer("");

          localStorage.removeItem("user");
          localStorage.removeItem("jwt");
          localStorage.removeItem("refresh");
          sessionStorage.removeItem("user");
          sessionStorage.removeItem("jwt");
          sessionStorage.removeItem("refresh");
        }
      }

      return response;
    } catch (error) {
      console.error("Error during request:", error);
      throw new Error("Request failed.");
    }
  };

  const refreshToken = async (): Promise<Response> => {
    let refreshToken;
    if (localStorage.getItem("refresh")) {
      refreshToken = localStorage.getItem("refresh");
    } else if (sessionStorage.getItem("refresh")) {
      refreshToken = sessionStorage.getItem("refresh");
    }

    if (!refreshToken) {
      throw new Error("No refresh token available");
    }

    const response = await fetch("https://localhost:7297/api/Auth/refresh", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(refreshToken),
    });

    return response;
  };

  const sendFriendRequest = async (uid: string, token: string) => {
    try {
      const response = await fetch(
        "https://localhost:7297/api/Friend/friend-request",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`,
          },
          body: JSON.stringify({ uid }),
        }
      );
      const data = await response.json();

      return data;
    } catch (error) {
      console.error("Error when sending friend request: " + error);
      return false;
    }
  };

  const acceptFriendRequest = async (
    uid: string,
    token: string
  ): Promise<ProfileData> => {
    try {
      const response = await fetch(
        "https://localhost:7297/api/Friend/accept-friend-request",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`,
          },
          body: JSON.stringify({ uid }),
        }
      );
      const data = await response.json();
      return data;
    } catch (error) {
      console.error("Error when sending friend request: " + error);
      return null;
    }
  };

  const deleteFriendRequest = async (
    uid: string,
    friendUid: string,
    token: string
  ): Promise<boolean> => {
    try {
      const response = await fetch(
        "https://localhost:7297/api/Friend/remove-friend",
        {
          method: "DELETE",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`,
          },
          body: JSON.stringify({ uid, friendUid }),
        }
      );
      const data = await response.json();
      return data;
    } catch (error) {
      console.error("Error when sending friend request: " + error);
      return false;
    }
  };

  const acceptFriendRequestInProfile = async (
    uid: string,
    friendUid: string,
    token: string
  ): Promise<ProfileData> => {
    try {
      const response = await fetch(
        "https://localhost:7297/api/Friend/accept-friend-profile",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`,
          },
          body: JSON.stringify({ uid, friendUid }),
        }
      );
      const data = await response.json();
      return data;
    } catch (error) {
      console.error("Error when sending friend request: " + error);
      return null;
    }
  };

  return (
    <AuthContext.Provider
      value={{
        uid,
        username,
        email,
        firstName,
        lastName,
        photoURL,
        jwt,
        refresh,
        selectedChat,
        setSelectedChat,
        isOwner,
        setIsOwner,
        selectedServer,
        setSelectedServer,
        theme,
        setTheme,
        loginAction,
        logOut,
        getProfileInfo,
        verifyJwt,
        registerAction,
        fetchRequest,
        sendFriendRequest,
        acceptFriendRequest,
        deleteFriendRequest,
        acceptFriendRequestInProfile,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
};

export default AuthProvider;
