// Import the functions you need from the SDKs you need
import { initializeApp } from "firebase/app";
import { getFirestore } from "firebase/firestore";
// TODO: Add SDKs for Firebase products that you want to use
// https://firebase.google.com/docs/web/setup#available-libraries

// Your web app's Firebase configuration
// Read sensitive values from environment variables. Configure these in your
// system or an .env file (not committed). Example .env.example is provided.
const firebaseConfig = {
  apiKey: process.env.FIREBASE_API_KEY || "REPLACE_WITH_FIREBASE_API_KEY",
  authDomain: process.env.FIREBASE_AUTH_DOMAIN || "REPLACE_WITH_AUTH_DOMAIN",
  databaseURL: process.env.FIREBASE_DATABASE_URL || "REPLACE_WITH_DATABASE_URL",
  projectId: process.env.FIREBASE_PROJECT_ID || "REPLACE_WITH_PROJECT_ID",
  storageBucket:
    process.env.FIREBASE_STORAGE_BUCKET || "REPLACE_WITH_STORAGE_BUCKET",
  messagingSenderId:
    process.env.FIREBASE_MESSAGING_SENDER_ID ||
    "REPLACE_WITH_MESSAGING_SENDER_ID",
  appId: process.env.FIREBASE_APP_ID || "REPLACE_WITH_APP_ID",
};

// Initialize Firebase
const app = initializeApp(firebaseConfig);
export const db = getFirestore(app);
