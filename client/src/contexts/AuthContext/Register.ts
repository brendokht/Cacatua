import { signupFormSchema } from "../../schemas/RegisterSchema";
import { z } from "zod";

export interface SignupReturnData {
  success: boolean;
  message?: string;
}

interface SignupResponseData {
  message: string;
}

export async function registerAction(
  values: z.infer<typeof signupFormSchema>
): Promise<SignupReturnData> {
  const registerBody = {
    displayName: values.username,
    email: values.email,
    firstname: values.firstname,
    lastname: values.lastname,
    password: values.password,
  };

  try {
    const response = await fetch(
      "https://localhost:7297/api/auth/pre-register",
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json;charset=UTF-8",
        },
        body: JSON.stringify(registerBody),
      }
    );

    const returnData: SignupReturnData = {
      success: response.ok,
    };

    const data: SignupResponseData = await response.json();

    returnData.message = data.message;

    return returnData;
  } catch (error) {
    console.error("Error during login:", error);
  }
}
