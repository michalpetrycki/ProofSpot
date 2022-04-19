import sys
import cv2
import numpy as np
import matplotlib.pyplot as plt
import matplotlib.ticker



image = cv2.imread(sys.argv[1])
output = image.copy()
height, width = image.shape[:2]

minRadius = 32
maxRadius = 35

tagName = 'TG-000-'
index = 1

# minRadius 33 and maxradius 34 seems to be optimal

gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
circles = cv2.HoughCircles(image=gray, 
                           method=cv2.HOUGH_GRADIENT, 
                           dp=1.2, 
                           minDist=2*minRadius,
                           param1=50,
                           param2=50,
                           minRadius=minRadius,
                           maxRadius=maxRadius                           
                          )

if circles is not None:
    # convert the (x, y) coordinates and radius of the circles to integers
    circlesRound = np.round(circles[0, :]).astype('int')
    # loop over the (x, y) coordinates and radius of the circles
    for (x, y, r) in circlesRound:
        # draw circle around the detected object
        cv2.circle(output, (x, y), r, (0, 255, 0), 4)

        # line - starting point
        pointA = (x - 30, y - 30)

        # line - ending point
        pointB = (x - 90, y - 90)

        # draw line from pointA to pointB, (r, g, b) color), thickness
        cv2.line(output, pointA, pointB, (136, 108, 210), thickness = 13)

        # draw rectangle as annotation box 
        cv2.rectangle(output, (x - 332, y - 172), (x - 95, y - 95), color = (255, 0, 0), thickness = 11)

        tag = ''
        if index < 10: tag = tagName + '00' + str(index)
        elif index < 100: tag = tagName + '0' + str(index)
        else: tag = tagName + str(index)

        # draw text (img, text, (coord_x, coord_y), font, font_size, font_color, font_thickness, line?)
        cv2.putText(output, tag, (x - 315, y - 122), cv2.FONT_HERSHEY_SIMPLEX, 1, (255, 0, 0), 4, cv2.LINE_AA)
        print(tag, x, y, r)

        index += 1

    plt.figure(figsize=(16, 10))
    plt.imshow(output, cmap = 'gray', interpolation = 'bicubic')
    plt.xticks([]), plt.yticks([])  # to hide tick values on X and Y axis
    plt.show()
else:
    print ('No circles found')
